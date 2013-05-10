using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Paxos
{
    public class NetworkMessage
    {
        //public for serialization

        public Guid MessageID = Guid.NewGuid();
        public Guid ReplyID;
        public MessageType Type;
        public SequenceNumber SequenceNumber;
        public string Value;
        public string RoundID;
        public Dictionary<string, string> DataSet;

        public NetworkMessage()
        {
        }
        private NetworkMessage(string RoundID, MessageType Type, SequenceNumber SequenceNo, string Value, Guid InResponseTo, Dictionary<string, string> DataSet = null)
        {
            this.RoundID = RoundID;
            this.Type = Type;
            this.SequenceNumber = SequenceNo;
            this.Value = Value;
            ReplyID = InResponseTo;
            this.DataSet = DataSet;
        }
        public override string ToString()
        {
            var result = Type.ToString() + "(";

            string valueStr = Value != null ? "'" + Value + "'" : "NULL";
            switch (Type)
            {
                case MessageType.Propose:
                case MessageType.Reject:
                case MessageType.Deny:
                    result += SequenceNumber.ToString();
                    break;
                case MessageType.Agree:
                    result += valueStr;
                    break;
                case MessageType.Commit:
                    result += SequenceNumber.ToString() + ", " + valueStr;
                    break;
            }

            return result + ") " + MessageID.ToString() + (ReplyID == Guid.Empty ? "" : " in reply to " + ReplyID);
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
            return new NetworkMessage(RoundID, MessageType.Accept, new SequenceNumber(), Value, MessageID);
        }
        public NetworkMessage Deny(SequenceNumber SequenceNumber)
        {
            return new NetworkMessage(RoundID, MessageType.Deny, SequenceNumber, null, MessageID);
        }

        public static NetworkMessage Initialize()
        {
            return new NetworkMessage(null, MessageType.Initialize, new SequenceNumber(), null, Guid.Empty);
        }
        public NetworkMessage Recover(Dictionary<string, string> DataSet)
        {
            return new NetworkMessage(null, MessageType.Recover, new SequenceNumber(), null, this.MessageID, DataSet);
        }
    }
    public enum MessageType
    {
        Propose,
        Agree,
        Reject,
        Commit,
        Accept,
        Deny,
        Initialize,
        Recover
    }
}
