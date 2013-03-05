using Paxos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterElection
{
    public class Master : IDuplexStream<NetworkMessage>
    {
        private readonly Proposer proposer;
        private readonly string networkAddress;

        private string master;
        private DateTime masterEnd = new DateTime(0);
        public bool IsMaster { get { return master == networkAddress && DateTime.UtcNow < masterEnd; } }

        public Master(string NetworkAddress, int Acceptors)
        {
            proposer = new Proposer(NetworkAddress, Acceptors);
            networkAddress = NetworkAddress;
        }

        private static Tuple<DateTime, DateTime> Round()
        {
            var now = DateTime.UtcNow;
            var millisecond = ((int)Math.Floor(((double)now.Millisecond / 500D))) * 500;
            var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, millisecond);
            var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, millisecond + 500);
            return new Tuple<DateTime, DateTime>(start, end);
        }
        private static Tuple<DateTime, DateTime> NextRound()
        {
            var now = DateTime.UtcNow;
            var millisecond = ((int)Math.Floor(((double)now.Millisecond / 500D))) * 500;
            var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, millisecond + 500);
            var end = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, millisecond + 1000);
            return new Tuple<DateTime, DateTime>(start, end);
        }
        private static bool RoundEqual(Tuple<DateTime, DateTime> RoundA, Tuple<DateTime, DateTime> RoundB)
        {
            return RoundA.Item1 == RoundB.Item1 && RoundA.Item2 == RoundB.Item2;
        }


        private async Task<string> FindMaster()
        {
            var round = Round();
            if (round.Item2 == masterEnd)
            {
                return master;
            }
            var winner = await proposer.Propose(round.Item1.Ticks.ToString(), networkAddress);
            master = winner;
            masterEnd = round.Item2;
            if (IsMaster)
            {
                var round2 = NextRound();
                var winner2 = await proposer.Propose(round2.Item1.Ticks.ToString(), networkAddress);
                master = winner;
                masterEnd = round2.Item2;
            }
            return master;
        }


        public void pipe(IWriteStream<NetworkMessage> stream)
        {
            proposer.pipe(stream);
        }
        public void SendMessage(string addressTo, string addressFrom, NetworkMessage message)
        {
            proposer.SendMessage(addressTo, addressFrom, message);
        }
    }
}
