using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos.AcceptorUtils
{
    class Round
    {
        public SequenceNumber AgreedSequenceNumber;
        public string AcceptedValue = null;
    }
}
