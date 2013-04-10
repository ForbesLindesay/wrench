using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paxos
{
    public struct SequenceNumber : IComparable<SequenceNumber>
    {
        public UInt32 Number;
        public string Address;
        public SequenceNumber(UInt32 Number, string Address)
        {
            this.Number = Number;
            this.Address = Address;
        }
        public SequenceNumber(string str)
        {
            str = str.Substring(1, str.Length - 2);
            this.Number = UInt32.Parse(str.Substring(0, str.IndexOf(':')));
            this.Address = str.Substring(str.IndexOf(':') + 1);
        }

        #region Comparison

        public int CompareTo(SequenceNumber other)
        {
            if (Number.CompareTo(other.Number) != 0)
            {
                return Number.CompareTo(other.Number);
            }
            else
            {
                return Address.CompareTo(other.Address);
            }
        }
        public override bool Equals(object obj)
        {
            if (obj is SequenceNumber)
            {
                return CompareTo((SequenceNumber)obj) == 0;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return Number.GetHashCode() * 2 + Address.GetHashCode() * 3;
        }

        public static bool operator >(SequenceNumber a, SequenceNumber b)
        {
            return a.CompareTo(b) > 0;
        }
        public static bool operator <(SequenceNumber a, SequenceNumber b)
        {
            return a.CompareTo(b) < 0;
        }
        public static bool operator <=(SequenceNumber a, SequenceNumber b)
        {
            return a.CompareTo(b) <= 0;
        }
        public static bool operator >=(SequenceNumber a, SequenceNumber b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator ==(SequenceNumber a, SequenceNumber b)
        {
            return a.CompareTo(b) == 0;
        }
        public static bool operator !=(SequenceNumber a, SequenceNumber b)
        {
            return !(a == b);
        }

        #endregion

        public override string ToString()
        {
            return "[" + Number.ToString() + ":" + Address + "]";
        }
    }
}
