using MasterElection;
using Paxos;
using StorageConnection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StorageNode
{
    public class StorageNode : IStorageNode
    {
        private static readonly JavaScriptSerializer JSON = new JavaScriptSerializer();

        private readonly ConcurrentDictionary<string, bool> AbortedTransactions = new ConcurrentDictionary<string, bool>();
        private readonly AsyncDictionary<string, long> Sequencing = new AsyncDictionary<string, long>();
        private readonly AsyncDictionary<string, Dictionary<string, string>> TransactionUpdates = new AsyncDictionary<string, Dictionary<string, string>>();
        private readonly BackingStore store = new BackingStore();

        private readonly string address;
        private readonly PaxosNode paxos;

        private readonly AsyncCounter CompletedTransactionID = new AsyncCounter(-1);
        private readonly AsyncCounter SequencedTransactionID = new AsyncCounter(-1);

        public StorageNode()
        {
            address = Guid.NewGuid().ToString();

            paxos.RoundComplete += paxos_RoundComplete;
            AbortedTransactions.TryAdd("SKIP", true);

            TransactionUpdates.KeyRequested += TransactionUpdates_KeyRequested;
        }

        void TransactionUpdates_KeyRequested(object sender, string key)
        {
            SendMessage("RequestResults", key);
        }

        #region Paxos Handler

        async void paxos_RoundComplete(object sender, RoundResult e)
        {
            long sequenceNumber;
            if (long.TryParse(e.RoundID, out sequenceNumber))
            {
                SequencedTransactionID.AutoIncrementOn(sequenceNumber);
                Sequencing.TrySet(e.Result, sequenceNumber);
                if (AbortedTransactions.ContainsKey(e.Result))
                {
                    OnTransactionSkip(sequenceNumber);
                }
            }
            else
            {
                if (e.Result == "COMMIT")
                {
                    OnTransactionCommited(await Sequencing.Get(e.RoundID), e.RoundID);
                }
                else
                {
                    AbortedTransactions.TryAdd(e.RoundID, true);
                    OnTransactionAborted(e.RoundID);
                    var sn = Sequencing.Get(e.RoundID);
                    if (sn.IsCompleted)
                    {
                        OnTransactionSkip(await sn);
                    }
                }
            }
        }

        private async void OnTransactionCommited(long SequenceNumber, string TransactionID)
        {
            await CompletedTransactionID.Wait(SequenceNumber - 1);
            var data = await TransactionUpdates.Get(TransactionID);
            foreach (var pair in data)
            {
                store.Set(SequenceNumber, pair.Key, pair.Value);
            }
            CompletedTransactionID.Increment();
        }
        private void OnTransactionAborted(string TransactionID)
        {
            TransactionUpdates.TryCancel(TransactionID);
            TransactionUpdates.Dispose(TransactionID);
        }
        private void OnTransactionSkip(long SequenceNumber)
        {
            CompletedTransactionID.AutoIncrementOn(SequenceNumber);
        }

        #endregion

        #region Internal Networking

        public void OnMessage(string Message)
        {
            var message = Message.Split(new[] { ':' }, 2);
            var method = message[0];
            var payload = message[1];
            Dictionary<string, string> updates;
            switch (method)
            {
                case "RequestResults":
                    if (TransactionUpdates.TryGet(payload, out updates))
                    {
                        SendMessage("Results", JSON.Serialize(new TransactionResults()
                            {
                                Initial = false,
                                TransactionID = payload,
                                Updated = updates
                            }));
                    }
                    break;
                case "Results":
                    var tr = JSON.Deserialize<TransactionResults>(payload);
                    TransactionUpdates.TrySet(tr.TransactionID, tr.Updated);
                    if (tr.Initial)
                    {
                        SendMessage("GotResults", JSON.Serialize(new TransactionConfirmation()
                            {
                                Address = address,
                                TransactionID = tr.TransactionID
                            }));
                    }
                    break;
                case "SequenceNumber":
                    var SequenceNumber = long.Parse(payload);
                    if (SequenceNumber > SequencedTransactionID.Current())
                    {
                        //find out about all the missing transactions
                        for (long i = SequencedTransactionID.Current(); i <= SequenceNumber; i++)
                        {
                            paxos.Propose(i.ToString(), "SKIP");
                        }
                    }
                    else if (SequenceNumber < SequencedTransactionID.Current())
                    {
                        //tell other nodes that they're out of date
                        SendMessage("SequenceNumber", SequencedTransactionID.Current().ToString());
                    }
                    break;
            }
        }
        private void SendMessage(string Method, string Payload)
        {
            if (Method.Contains(':')) throw new ArgumentException("Method can't contain `:`");
            var message = Method + ":" + Payload;
        }

        #endregion

        #region Read Only Transactions

        public Task<ReadID> BeginReadTransaction()
        {
            //always assume just start from the latest committed transaction
            return Task.FromResult(new ReadID() { sequenceNumber = CompletedTransactionID.Current() });
        }

        public async Task<string> Read(ReadID ReadTransactionID, string Key)
        {
            //Read at a point in time
            var SequenceNumber = ReadTransactionID.sequenceNumber;

            //wait for transaction to complete
            await CompletedTransactionID.Wait(SequenceNumber);

            //get value or return null if it doesn't exist
            return store.Get(SequenceNumber, Key);
        }

        #endregion

        #region Write Transactions

        public Task<WriteID> BeginWriteTransaction(string[] Keys)
        {
            var lastCommitted = CompletedTransactionID.Current();
            var transactionID = Guid.NewGuid();
            return Task.FromResult(new WriteID()
            {
                sequenceNumber = lastCommitted,
                transactionID = transactionID.ToString()
            });
        }

        public async Task Commit(WriteID WriteTransactionID, Dictionary<string, string> Updated)
        {
            //obtain a sequence number
            long startSequenceNumber = WriteTransactionID.sequenceNumber;
            long endSequenceNumber = -1;
            string id = null;
            while (id != WriteTransactionID.transactionID)
            {
                endSequenceNumber = SequencedTransactionID.Increment();
                id = await paxos.Propose(endSequenceNumber.ToString(), WriteTransactionID.transactionID);
            }

            for (int i = 0; i < endSequenceNumber; i++)
			{
			    //todo: check for conflicts and abort if conflicts
			}

            //todo: distribute the contents of the transactions updates
            SendMessage("Results", JSON.Serialize(new TransactionResults()
            {
                Initial = true,
                TransactionID = WriteTransactionID.transactionID,
                Updated = Updated
            }));

            if ("COMMIT" == await paxos.Propose(WriteTransactionID.transactionID, "COMMIT"))
            {
                //wait for the transactions results to propagate back to here
                await CompletedTransactionID.Wait(endSequenceNumber);
            }
            else
            {
                throw new Exception("Transaction failed to commit as it was already aborted.");
            }
        }

        public async Task Abort(WriteID WriteTransactionID)
        {
            if ("ABORT" == await paxos.Propose(WriteTransactionID.transactionID, "ABORT"))
            {
                return; //success
            }
            else
            {
                throw new Exception("Transaction failed to abort as it was already committed.");
            }
        }

        #endregion


    }
    public class TransactionResults
    {
        public bool Initial;
        public string TransactionID;
        public Dictionary<string, string> Updated;
    }
    public class TransactionConfirmation
    {
        public string Address;
        public string TransactionID;
    }
}
