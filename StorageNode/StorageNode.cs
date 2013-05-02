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

        private readonly AsyncDictionary<long, string> Sequencing = new AsyncDictionary<long, string>();
        private readonly AsyncDictionary<string, bool> TransactionResults = new AsyncDictionary<string, bool>();
        private readonly AsyncDictionary<string, Dictionary<string, string>> TransactionUpdates = new AsyncDictionary<string, Dictionary<string, string>>();
        private readonly ConcurrentDictionary<string, AsyncSet<string>> TransactionDistribution = new ConcurrentDictionary<string, AsyncSet<string>>();
        private readonly BackingStore store = new BackingStore();

        private readonly string address;
        private readonly PaxosNode paxos;

        private readonly AsyncCounter CompletedTransactionID = new AsyncCounter(-1);
        private readonly AsyncCounter SequencedTransactionID = new AsyncCounter(-1);

        public StorageNode(int NodeCount)
        {
            address = Guid.NewGuid().ToString();
            paxos = new PaxosNode(address, NodeCount);

            paxos.RoundComplete += paxos_RoundComplete;
            TransactionResults.TrySet("SKIP", false);
            Sequencing.TrySet(-1, "SKIP");

            TransactionUpdates.KeyRequested += TransactionUpdates_KeyRequested;

            paxos.Message += (s, m) => SendMessage("paxos", m);
        }

        void TransactionUpdates_KeyRequested(object sender, string key)
        {
            SendMessage("RequestResults", key);
        }

        #region Paxos Handler

        private int pendingTransactions = 0;
        async void paxos_RoundComplete(object sender, RoundResult e)
        {
            long sequenceNumber;
            if (long.TryParse(e.RoundID, out sequenceNumber))
            {
                SequencedTransactionID.AutoIncrementOn(sequenceNumber);
                Sequencing.TrySet(sequenceNumber, e.Result);
                pendingTransactions++;
                var committed = await TransactionResults.Get(e.Result);
                pendingTransactions--;
                if (committed)
                {
                    OnTransactionCommited(sequenceNumber, e.Result);
                }
                else
                {
                    CompletedTransactionID.AutoIncrementOn(sequenceNumber);
                }
            }
            else
            {
                if (e.Result == "COMMIT")
                {
                    TransactionResults.TrySet(e.RoundID, true);
                }
                else
                {
                    TransactionUpdates.Dispose(e.RoundID);
                    TransactionResults.TrySet(e.RoundID, false);
                }
                if (pendingTransactions == 0)
                {
                    //check we're not behind on sequence numbers
                    SendMessage("SequenceNumber", SequencedTransactionID.Current().ToString());
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
                case "paxos":
                    paxos.OnMessage(payload);
                    break;
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
                case "GotResults":
                    var tc = JSON.Deserialize<TransactionConfirmation>(payload);
                    AsyncSet<string> distribution;
                    if (TransactionDistribution.TryGetValue(tc.TransactionID, out distribution))
                    {
                        distribution.Add(tc.Address);
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
            var handler = Message;
            if (handler != null)
            {
                handler(this, message);
            }
        }

        public event EventHandler<string> Message;
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

        public async Task Commit(WriteID WriteTransactionID, Dictionary<string, string> Updated, String[] Read)
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

            for (long i = startSequenceNumber + 1; i < endSequenceNumber; i++)
			{
                var tid = await Sequencing.Get(i);
                if (tid != "SKIP")
                {
                    var updates = await TransactionUpdates.Get(tid);
                    var updated = updates.Keys;
                    var read = new HashSet<string>(Read);
                    foreach (var key in updated)
                    {
                        if (read.Contains(key))
                        {
                            //if we read a key that another transaction wrote, and we missed that write, we need to abort
                            await Abort(WriteTransactionID);
                            throw new Exception("Transaction aborted due to conflict.");
                        }

                    }
                }
			}

            // distribute the contents of the transactions updates
            var distribution = TransactionDistribution.GetOrAdd(WriteTransactionID.transactionID, new AsyncSet<string>());
            TransactionUpdates.TrySet(WriteTransactionID.transactionID, Updated);
            distribution.Add(address);
            SendMessage("Results", JSON.Serialize(new TransactionResults()
            {
                Initial = true,
                TransactionID = WriteTransactionID.transactionID,
                Updated = Updated
            }));
            await distribution.Wait((int)Math.Ceiling(paxos.NumberOfNodes / 2D));

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
