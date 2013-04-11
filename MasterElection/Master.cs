﻿using Paxos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterElection
{
    class Master
    {
        private readonly PaxosNode node;
        public readonly string Address;
        public int NumberOfNodes { get { return node.NumberOfNodes; } set { node.NumberOfNodes = value; } }

        public readonly TimeSpan DriftRange;
        public readonly int LeaseSpanMinutes;

        public Master(string Address, int NumberOfNodes, TimeSpan DriftRange, int LeaseSpanMinutes)
        {
            if (LeaseSpanMinutes > 60)
                throw new ArgumentOutOfRangeException("LeaseSpanMinutes", LeaseSpanMinutes, "LeaseSpan must be less than 60 minutes");
            if (!IsDivisble(LeaseSpanMinutes, 60))
                throw new ArgumentException("LeaseSpan must divide exactly into an hour", "LeaseSpanMinutes");
            this.Address = Address;
            this.NumberOfNodes = NumberOfNodes;
            this.DriftRange = DriftRange;
            this.LeaseSpanMinutes = LeaseSpanMinutes;
            node = new PaxosNode(Address, NumberOfNodes);
        }

        public bool TryGetMaster(string LeaseID, out string Address)
        {
            return node.TryGetResult(LeaseID, out Address);
        }
        public bool TryGetMaster(out string Address)
        {
            var now = DateTime.UtcNow;
            var lower = now - new TimeSpan(DriftRange.Ticks / 2L);
            var upper = lower + DriftRange;

            var start = new DateTime(lower.Year, lower.Month, lower.Day, lower.Hour, 0, 0, DateTimeKind.Utc);
            while (start < lower)
            {
                start = start.AddMinutes(LeaseSpanMinutes);
            }
            var end = (new DateTime(upper.Year, upper.Month, upper.Day, upper.Hour, 0, 0, DateTimeKind.Utc)).AddHours(1);
            while (end > upper)
            {
                end = end.AddMinutes(-LeaseSpanMinutes);
            }
            var startID = start.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
            var endID = end.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
            if (startID == endID) return TryGetMaster(startID, out Address);
            string before;
            string after;
            if (TryGetMaster(startID, out before) && TryGetMaster(endID, out after))
            {
                if (before == after)
                {
                    Address = before;
                    return true;
                }
            }
            Address = "";
            return false;
        }

        public event EventHandler<string> Message
        {
            add { node.Message += value; }
            remove { node.Message -= value; }
        }
        public void OnMessage(string Message)
        {
            node.OnMessage(Message);
        }


        public static bool IsDivisble(int x, int n)
        {
            return (x % n) == 0;
        }
    }
}
