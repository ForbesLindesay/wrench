using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paxos
{
    public class NetworkMessage
    {
        private readonly Guid messageID = Guid.NewGuid();
        private readonly Guid inResponseTo;
        private readonly MessageType type;
        private readonly SequenceNumber SID;
        private readonly string value;
        private readonly string roundID;

        public MessageType Type { get { return type; } }
        public SequenceNumber SequenceNumber { get { return SID; } }
        public string Value { get { return value; } }
        public Guid MessageID { get { return messageID; } }
        public Guid ReplyID { get { return inResponseTo; } }
        public string RoundID { get { return roundID; } }

        private NetworkMessage(string RoundID, MessageType Type, SequenceNumber SequenceNo, string Value, Guid InResponseTo)
        {
            roundID = RoundID;
            type = Type;
            SID = SequenceNo;
            value = Value;
            inResponseTo = InResponseTo;
        }
        public override string ToString()
        {
            var result = type.ToString() + "(";

            string valueStr = value != null ? "'" + value + "'" : "NULL";
            switch (type)
            {
                case MessageType.Propose:
                case MessageType.Reject:
                case MessageType.Deny:
                    result += SID.ToString();
                    break;
                case MessageType.Agree:
                    result += valueStr;
                    break;
                case MessageType.Commit:
                    result += SID.ToString() + ", " + valueStr;
                    break;
            }

            return result + ") " + MessageID.ToString() + (inResponseTo == Guid.Empty ? "" : " in reply to " + inResponseTo);
        }

        public static NetworkMessage Propose(string RoundID, SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(RoundID, MessageType.Propose, SequenceNumber, null, Guid.Empty);
        }
        public NetworkMessage Agree(string Value)
        {
            return new NetworkMessage(RoundID, MessageType.Agree, new SequenceNumber(), Value, MessageID);
        }
        public NetworkMessage Reject(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(RoundID, MessageType.Reject, SequenceNumber, null, MessageID);
        }
        public static NetworkMessage Commit(string RoundID, SequenceNumber SequenceNumber, string Value)
        {
            return new NetworkMessage(RoundID, MessageType.Commit, SequenceNumber, Value, Guid.Empty);
        }
        public NetworkMessage Accept()
        {
            return new NetworkMessage(RoundID, MessageType.Accept, new SequenceNumber(), value, MessageID);
        }
        public NetworkMessage Deny(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(RoundID, MessageType.Deny, SequenceNumber, null, MessageID);
        }

    }
    public enum MessageType
    {
        Propose,
        Agree,
        Reject,
        Commit,
        Accept,
        Deny
    }
}
