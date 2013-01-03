using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paxos
{
    public struct SequenceNumber : IComparable<SequenceNumber>
    {
        private readonly UInt32 number;
        private readonly string address;

        public SequenceNumber(UInt32 Number, string Address)
        {
            number = Number;
            address = Address;
        }
        public SequenceNumber(string str)
        {
            str = str.Substring(1, str.Length - 2);
            number = UInt32.Parse(str.Substring(0, str.IndexOf(':')));
            address = str.Substring(str.IndexOf(':') + 1);
        }

        public UInt32 Number { get { return number; } }
        public string Address { get { return address; } }

        #region Comparison

        public int CompareTo(SequenceNumber other)
        {
            if (number.CompareTo(other.number) != 0)
            {
                return number.CompareTo(other.number);
            }
            else
            {
                return address.CompareTo(other.address);
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
            return number.GetHashCode() * 2 + address.GetHashCode() * 3;
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
