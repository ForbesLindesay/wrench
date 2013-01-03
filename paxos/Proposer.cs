using NAct;
using System;
using System.Collections.Generic;
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

        private readonly List<IWriteStream<NetworkMessage>> outputs = new List<IWriteStream<NetworkMessage>>();
        private readonly Dictionary<Guid, ITaskQueue<NetworkMessage>> responses = new Dictionary<Guid, ITaskQueue<NetworkMessage>>();
        private ITaskQueue<NetworkMessage> Broadcast(NetworkMessage message)
        {
            responses[message.MessageID] = new TaskQueue<NetworkMessage>();
            foreach (var output in outputs)
            {
                output.SendMessage("BROADCAST", address, message);
            }
            return responses[message.MessageID];
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
                    var proposeResponse = Broadcast(NetworkMessage.Propose(SID));
                    int agreements = 0;
                    int disagreements = 0;
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

                    if (agreements >= quorum)
                    {
                        var commitResponse = Broadcast(NetworkMessage.Commit(SID, value));
                        int accept = 0;
                        int deny = 0;
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
            if (responses.ContainsKey(message.ReplyID))
            {
                responses[message.ReplyID].Enqueue(message);
            }
        }
    }
}
