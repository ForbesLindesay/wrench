using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Paxos.AcceptorUtils;

namespace Paxos
{
    public interface IAcceptor : IDuplexStream<NetworkMessage>
    {
        ProposalResponse Propose(string RoundID, SequenceNumber sequenceNumber);
        CommitResponse Commit(string RoundID, SequenceNumber sequenceNumber, string proposedValue);
    }
    public class Acceptor : IAcceptor
    {
        private readonly string address;
        private readonly Learner learner;

        public Acceptor(string Address, Learner Learner = null)
        {
            address = Address;
            learner = Learner;
        }

        #region Acceptor

        private readonly Dictionary<string, Round> rounds = new Dictionary<string, Round>();

        readonly object locker = new Object();

        public ProposalResponse Propose(string RoundID, SequenceNumber sequenceNumber)
        {
            lock (locker)
            {
                if (!rounds.ContainsKey(RoundID)) rounds.Add(RoundID, new Round());
                Round round = rounds[RoundID];
                string result;
                if (learner != null && learner.TryGetResult(RoundID, out result))
                {
                    round.AcceptedValue = result;
                }

                if (sequenceNumber > round.AgreedSequenceNumber)
                {
                    round.AgreedSequenceNumber = sequenceNumber;
                    return ProposalResponse.Agree(round.AcceptedValue);
                }
                else
                {
                    return ProposalResponse.Reject(round.AgreedSequenceNumber);
                }
            }
        }

        public CommitResponse Commit(string RoundID, SequenceNumber sequenceNumber, string proposedValue)
        {
            lock (locker)
            {
                if (!rounds.ContainsKey(RoundID)) rounds.Add(RoundID, new Round());
                Round round = rounds[RoundID];
                string result;
                if (learner != null && learner.TryGetResult(RoundID, out result))
                {
                    round.AcceptedValue = result;
                }

                if (sequenceNumber == round.AgreedSequenceNumber && (round.AcceptedValue == null || round.AcceptedValue == proposedValue) && proposedValue != null)
                {
                    round.AcceptedValue = proposedValue;
                    return CommitResponse.Accept();
                }
                else
                {
                    return CommitResponse.Deny(round.AgreedSequenceNumber);
                }
            }
        }

        #endregion

        #region Stream

        private NetworkMessage Handle(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.Propose:
                    var pResult = Propose(message.RoundID, message.SequenceNumber);
                    if (pResult.IsAgreed)
                    {
                        return message.Agree(pResult.AgreedProposal);
                    }
                    else
                    {
                        return message.Reject(pResult.HighestAgreedSequenceNumber);
                    }
                case MessageType.Commit:
                    var cResult = Commit(message.RoundID, message.SequenceNumber, message.Value);
                    if (cResult.IsAccepted)
                    {
                        return message.Accept();
                    }
                    else
                    {
                        return message.Deny(cResult.HighestAgreedSequenceNumber);
                    }
                default:
                    return null;
            }
        }

        public async void SendMessage(string addressTo, string addressFrom, NetworkMessage message)
        {
            await Task.Yield();//prevent stack overflow
            var response = Handle(message);
            if (response != null) SendReply(addressFrom, response);
        }
        private void SendReply(string addressTo, NetworkMessage message)
        {
            foreach (var output in outputs)
            {
                output.SendMessage(addressTo, address, message);
            }
        }

        private readonly List<IWriteStream<NetworkMessage>> outputs = new List<IWriteStream<NetworkMessage>>();
        public void pipe(IWriteStream<NetworkMessage> stream)
        {
            outputs.Add(stream);
        }

        #endregion
    }
}
