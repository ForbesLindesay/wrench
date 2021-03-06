﻿using Paxos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterElection
{
    public class Master
    {
        private readonly PaxosNode node;
        public string Address { get { return node.Address; } }
        public int NumberOfNodes { get { return node.NumberOfNodes; } set { node.NumberOfNodes = value; } }

        public readonly TimeSpan DriftRange;
        public readonly int LeaseSpanSeconds;

        //Can be set to `false` for testing purposes
        public bool Connected = true;

        public Master(PaxosNode Node, TimeSpan DriftRange, TimeSpan LeaseSpan)
        {
            node = Node;
            if (LeaseSpan > TimeSpan.FromMinutes(1))
                throw new ArgumentOutOfRangeException("LeaseSpan", LeaseSpan, "LeaseSpan must be less than 60 minutes");
            LeaseSpanSeconds = (int)Math.Floor(LeaseSpan.TotalSeconds);
            if (!IsDivisble(LeaseSpanSeconds, 60 * 60))
                throw new ArgumentException("LeaseSpan must divide exactly into an hour", "LeaseSpan");
            if (DriftRange > LeaseSpan)
                throw new ArgumentOutOfRangeException("DriftRange", DriftRange, "DriftRange can't be more than the LeaseSpan");
            this.DriftRange = DriftRange;
        }

        public Master(string Address, int NumberOfNodes, TimeSpan DriftRange, TimeSpan LeaseSpan)
            :this(new PaxosNode(Address, NumberOfNodes), DriftRange, LeaseSpan)
        {
        }

        private Task<string> GetMaster(string LeaseID)
        {
            return node.GetResult(LeaseID);
        }
        private bool TryGetMaster(string LeaseID, out string Address)
        {
            return node.TryGetResult(LeaseID, out Address);
        }
        private DateTime GetStartTime(DateTime now)
        {
            var lower = now - new TimeSpan(DriftRange.Ticks / 2L);
            var start = new DateTime(lower.Year, lower.Month, lower.Day, lower.Hour, 0, 0, DateTimeKind.Utc);
            while (start < lower)
            {
                start = start.AddSeconds(LeaseSpanSeconds);
            }
            return start;
        }
        private DateTime GetEndTime(DateTime now)
        {
            var upper = now + new TimeSpan(DriftRange.Ticks / 2L);
            var end = (new DateTime(upper.Year, upper.Month, upper.Day, upper.Hour, 0, 0, DateTimeKind.Utc)).AddHours(1);
            while (end > upper)
            {
                end = end.AddSeconds(-LeaseSpanSeconds);
            }
            return end;
        }
        private string GetStartID(DateTime now)
        {
            return GetStartTime(now).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
        }
        private string GetNextID(DateTime now)
        {
            return GetStartTime(now).AddSeconds(LeaseSpanSeconds).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
        }
        private string GetEndID(DateTime now)
        {
            return GetEndTime(now).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
        }
        public bool TryGetMaster(out string Address)
        {
            var now = DateTime.UtcNow;
            var startID = GetStartID(now);
            var endID = GetEndID(now);

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
            else
            {
                TryElect();
            }
            Address = "";
            return false;
        }
        public async Task<string> GetMaster()
        {
            string master;
            while (!TryGetMaster(out master))
            {
                if (!TryGetMaster(GetStartID(DateTime.UtcNow), out master))
                {
                    await TryElect();
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var ts = GetEndTime(now) - now;
                    if (ts.Ticks > 0)
                    {
                        await Task.Delay(ts);
                    }
                    else
                    {
                        await Task.Yield();
                    }
                }
            }
            return master;
        }
        private async Task<bool> TryReElect()
        {
            var now = DateTime.UtcNow;
            string master;
            if (TryGetMaster(GetStartID(now), out master) && master == Address)
            {
                return Address == await node.Propose(GetNextID(now), Address);
            }
            return false;
        }
        private async void KeepElected()
        {
            while (await TryReElect())
            {
                var now = DateTime.UtcNow;
                var ts = GetStartTime(now).AddSeconds(LeaseSpanSeconds) - now;
                if (ts.Ticks > 0)
                {
                    await Task.Delay(ts);
                }
            }
        }

        private async Task<bool> TryElect()
        {
            var now = DateTime.UtcNow;
            if (Address == await node.Propose(GetStartID(now), Address))
            {
                string master;
                KeepElected();
                if (TryGetMaster(out master))
                {
                    return master == Address;
                }
            }
            return false;
        }

        public event EventHandler<string> Message
        {
            add { node.Message += value; }
            remove { node.Message -= value; }
        }
        public void OnMessage(string Message)
        {
            if (Connected) node.OnMessage(Message);
        }


        private static bool IsDivisble(int x, int n)
        {
            return (n % x) == 0;
        }
    }
}
