using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos.AcceptorUtils
{
    public class CommitResponse
    {
        private readonly bool accepted;
        private readonly SequenceNumber number;

        private CommitResponse(SequenceNumber Number)
        {
            accepted = false;
            number = Number;
        }
        private CommitResponse()
        {
            accepted = true;
        }

        public bool IsAccepted { get { return accepted; } }
        public SequenceNumber HighestAgreedSequenceNumber { get { return number; } }

        public static CommitResponse Accept()
        {
            return new CommitResponse();
        }
        public static CommitResponse Deny(SequenceNumber HighestAgreedSequenceNumber)
        {
            return new CommitResponse(HighestAgreedSequenceNumber);
        }

        public override string ToString()
        {
            return IsAccepted ? "Accept()" : "Deny('" + HighestAgreedSequenceNumber.ToString() + "')";
        }
    }
}
