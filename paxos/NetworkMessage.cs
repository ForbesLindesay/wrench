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

        public MessageType Type { get { return type; } }
        public SequenceNumber SequenceNumber { get { return SID; } }
        public string Value { get { return value; } }
        public Guid MessageID { get { return messageID; } }
        public Guid ReplyID { get { return inResponseTo; } }

        private NetworkMessage(MessageType Type, SequenceNumber SequenceNo, string Value, Guid InResponseTo)
        {
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

        public static NetworkMessage Propose(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(MessageType.Propose, SequenceNumber, null, Guid.Empty);
        }
        public NetworkMessage Agree(string Value)
        {
            return new NetworkMessage(MessageType.Agree, new SequenceNumber(), Value, MessageID);
        }
        public NetworkMessage Reject(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(MessageType.Reject, SequenceNumber, null, MessageID);
        }
        public static NetworkMessage Commit(SequenceNumber SequenceNumber, string Value)
        {
            return new NetworkMessage(MessageType.Commit, SequenceNumber, Value, Guid.Empty);
        }
        public NetworkMessage Accept()
        {
            return new NetworkMessage(MessageType.Accept, new SequenceNumber(), null, MessageID);
        }
        public NetworkMessage Deny(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(MessageType.Deny, SequenceNumber, null, MessageID);
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
