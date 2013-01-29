using System;
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
    }

    public class Proposer : IProposer
    {
        private readonly int totalAcceptors;
        private readonly int quorum;
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
            quorum = (Acceptors / 2) + 1;
            totalAcceptors = Acceptors;
        }

        public Task<string> Propose(string value)
        {
            return Propose(value, CancellationToken.None);
        }
        public async Task<string> Propose(string value, CancellationToken cancellationToken)
        {
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
                    using (var proposeResponse = Broadcast(NetworkMessage.Propose(SID)))
                    {
                        while (agreements < quorum && disagreements < quorum && (disagreements + agreements) < totalAcceptors)
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

                    if (agreements >= quorum)
                    {
                        var accept = 0;
                        var deny = 0;
                        using (var commitResponse = Broadcast(NetworkMessage.Commit(SID, value)))
                        {
                            while (accept < quorum && deny < quorum && (accept + deny) < totalAcceptors)
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
                        if (accept >= quorum)
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
