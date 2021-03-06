﻿using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos
{
    public interface IProposer : IDuplexStream<NetworkMessage>
    {
        Task<string> Propose(string value);
        Task<string> Propose(string value, CancellationToken cancellationToken);
        Task<string> Propose(string RoundID, string value);
        Task<string> Propose(string RoundID, string value, CancellationToken cancellationToken);
    }

    public class Proposer : IProposer
    {
        private int totalAcceptors;
        public int TotalAcceptors
        {
            get { return totalAcceptors; }
            set { totalAcceptors = value; }
        }
        public int Quorum
        {
            get { return (TotalAcceptors / 2) + 1; }
        }
        private readonly string address;

        private readonly ConcurrentBag<IWriteStream<NetworkMessage>> outputs = new ConcurrentBag<IWriteStream<NetworkMessage>>();
        private readonly ConcurrentDictionary<Guid, ITaskQueue<NetworkMessage>> responses = new ConcurrentDictionary<Guid, ITaskQueue<NetworkMessage>>();
        private IDisposableTaskQueue<NetworkMessage> Broadcast(NetworkMessage message)
        {
            var queue = responses.GetOrAdd(message.MessageID, new TaskQueue<NetworkMessage>());
            foreach (var output in outputs)
            {
                output.SendMessage("BROADCAST", address, message);
            }
            return new DisposableTaskQueue<NetworkMessage>(queue, () => responses.TryRemove(message.MessageID, out queue));
        }

        public Proposer(string NetworkAddress, int Acceptors)
        {
            address = NetworkAddress;
            totalAcceptors = Acceptors;
        }

        #region Overloads

        public Task<string> Propose(string value)
        {
            return Propose(value, CancellationToken.None);
        }
        public Task<string> Propose(string value, CancellationToken cancellationToken)
        {
            return Propose("-", value, cancellationToken);
        }
        public Task<string> Propose(string RoundID, string value)
        {
            return Propose(RoundID, value, CancellationToken.None);
        }

        #endregion

        public async Task<string> Propose(string RoundID, string value, CancellationToken cancellationToken)
        {
            if (RoundID == null) throw new ArgumentNullException("RoundID");
            if (value == null) throw new ArgumentNullException("Value");
            if (cancellationToken == null) throw new ArgumentNullException("cancellationToken");
            
            SequenceNumber highestNumber = new SequenceNumber();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (highestNumber.Address != null && highestNumber.Address.CompareTo(address) > 0)
                    {
                        await Task.Delay(1000);
                    }
                    var SID = new SequenceNumber(highestNumber.Number + 1, address);
                    var agreements = 0;
                    var disagreements = 0;
                    using (var proposeResponse = Broadcast(NetworkMessage.Propose(RoundID, SID)))
                    {
                        while (agreements < Quorum && disagreements < Quorum && (disagreements + agreements) < totalAcceptors)
                        {
                            var message = await proposeResponse.Dequeue(TimeSpan.FromSeconds(5), cancellationToken);
                            if (message.Type == MessageType.Agree)
                            {
                                agreements++;
                                if (message.Value != null) value = message.Value;
                            }
                            else if (message.Type == MessageType.Reject)
                            {
                                disagreements++;
                                if (message.SequenceNumber > highestNumber) highestNumber = message.SequenceNumber;
                            }
                        }
                    }

                    if (agreements >= Quorum)
                    {
                        var accept = 0;
                        var deny = 0;
                        using (var commitResponse = Broadcast(NetworkMessage.Commit(RoundID, SID, value)))
                        {
                            while (accept < Quorum && deny < Quorum && (accept + deny) < totalAcceptors)
                            {
                                var message = await commitResponse.Dequeue(TimeSpan.FromSeconds(5), cancellationToken);
                                if (message.Type == MessageType.Accept)
                                {
                                    accept++;
                                }
                                else if (message.Type == MessageType.Deny)
                                {
                                    deny++;
                                    if (message.SequenceNumber > highestNumber) highestNumber = message.SequenceNumber;
                                }
                            }
                        }
                        if (accept >= Quorum)
                        {
                            return value;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    //retry
                }
            }
            throw new OperationCanceledException(cancellationToken);
        }

        public void pipe(IWriteStream<NetworkMessage> stream)
        {
            outputs.Add(stream);
        }

        public void SendMessage(string addressTo, string addressFrom, NetworkMessage message)
        {
            ITaskQueue<NetworkMessage> queue;
            if (responses.TryGetValue(message.ReplyID, out queue))
            {
                queue.Enqueue(message);
            }
        }
    }
}