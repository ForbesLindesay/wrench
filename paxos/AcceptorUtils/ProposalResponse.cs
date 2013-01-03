using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paxos.AcceptorUtils
{
    public class ProposalResponse
    {
        private readonly bool agreed;
        private readonly SequenceNumber number;
        private readonly string value;

        private ProposalResponse(SequenceNumber Number)
        {
            agreed = false;
            number = Number;
        }
        private ProposalResponse(string Value)
        {
            agreed = true;
            value = Value;
        }

        public bool IsAgreed { get { return agreed; } }
        public SequenceNumber HighestAgreedSequenceNumber { get { return number; } }
        public string AgreedProposal { get { return value; } }

        public static ProposalResponse Agree(string AcceptedValue)
        {
            return new ProposalResponse(AcceptedValue);
        }
        public static ProposalResponse Reject(SequenceNumber HighestAgreedSequenceNumber)
        {
            return new ProposalResponse(HighestAgreedSequenceNumber);
        }

        public override string ToString()
        {
            return IsAgreed ? "Agree('" + AgreedProposal + "')" : "Reject('" + HighestAgreedSequenceNumber.ToString() + "')";
        }
    }
}
