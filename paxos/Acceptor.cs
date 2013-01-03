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
        ProposalResponse Propose(SequenceNumber sequenceNumber);
        CommitResponse Commit(SequenceNumber sequenceNumber, string proposedValue);
    }
    public class Acceptor : IAcceptor
    {

        #region Acceptor

        SequenceNumber agreedSequenceNumber;
        string acceptedValue = null;
        readonly object locker = new Object();

        public ProposalResponse Propose(SequenceNumber sequenceNumber)
        {
            lock (locker)
            {
                if (sequenceNumber > agreedSequenceNumber)
                {
                    agreedSequenceNumber = sequenceNumber;
                    return ProposalResponse.Agree(acceptedValue);
                }
                else
                {
                    return ProposalResponse.Reject(agreedSequenceNumber);
                }
            }
        }

        public CommitResponse Commit(SequenceNumber sequenceNumber, string proposedValue)
        {
            lock (locker)
            {
                if (sequenceNumber == agreedSequenceNumber && (acceptedValue == null || acceptedValue == proposedValue) && proposedValue != null)
                {
                    acceptedValue = proposedValue;
                    return CommitResponse.Accept();
                }
                else
                {
                    return CommitResponse.Deny(agreedSequenceNumber);
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
                    var pResult = Propose(message.SequenceNumber);
                    if (pResult.IsAgreed)
                    {
                        return message.Agree(pResult.AgreedProposal);
                    }
                    else
                    {
                        return message.Reject(pResult.HighestAgreedSequenceNumber);
                    }
                case MessageType.Commit:
                    var cResult = Commit(message.SequenceNumber, message.Value);
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

        public void SendMessage(string addressTo, string addressFrom, NetworkMessage message)
        {
            var response = Handle(message);
            if (response != null) SendReply(addressFrom, response);
        }
        private void SendReply(string addressTo, NetworkMessage message)
        {
            foreach (var output in outputs)
            {
                output.SendMessage(addressTo, "", message);
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
