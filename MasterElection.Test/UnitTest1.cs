using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MasterElection.Test
{
    [TestClass]
    public class MasterElectionTests
    {
        [TestMethod]
        public async Task TestMasterElection()
        {
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            Link(nodeA, nodeB, nodeC);
            var A = nodeA.GetMaster();
            var B = nodeB.GetMaster();
            var C = nodeC.GetMaster();
            Assert.AreEqual(await A, await B);
            Assert.AreEqual(await B, await C);
            MastersMatch(nodeA, nodeB, nodeC);
        }
        public void Link(params Master[] nodes)
        {
            foreach (var node in nodes)
            {
                foreach (var dest in nodes)
                {
                    LinkFrom(node, dest);
                }
            }
        }
        public void LinkFrom(Master source, Master dest)
        {
            source.Message += (s, m) => dest.OnMessage(m);
        }
        public bool MastersMatch(params Master[] nodes)
        {
            string master = null;
            foreach (var node in nodes)
            {
                if (master != null)
                {
                    string result;
                    if (node.TryGetMaster(out result))
                    {
                        Assert.AreEqual(master, result);
                    }
                }
                else
                {
                    string result;
                    if (node.TryGetMaster(out result))
                    {
                        master = result;
                    }
                }
            }
            return master != null;
        }
        public void MastersLive(string master, params Master[] nodes)
        {
            foreach (var node in nodes)
            {
                string result;
                if (node.TryGetMaster(out result))
                {
                    Assert.AreEqual(master, result);
                }
                else
                {
                    Assert.Fail("Not all nodes were live");
                }
            }
        }

        //This test demonstrates that once elected, a master is maintained for the duration
        [TestMethod]
        public async Task MasterElectionIntegrationTest()
        {
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
            Link(nodeA, nodeB, nodeC);
            var A = nodeA.GetMaster();
            var B = nodeB.GetMaster();
            var C = nodeC.GetMaster();
            await A; await B; await C;
            //sometimes two different masters get elected for the first two rounds
            //when two nodes startup very close together so wait until we've checked
            //the master a second time
            A = nodeA.GetMaster();
            B = nodeB.GetMaster();
            C = nodeC.GetMaster();
            Assert.AreEqual(await A, await B);
            Assert.AreEqual(await B, await C);

            var master = await A;

            DateTime start = DateTime.UtcNow;
            while (DateTime.UtcNow.Subtract(start) < TimeSpan.FromSeconds(30))
            {
                await Task.Delay(200);
                MastersLive(master, nodeA, nodeB, nodeC);
                Console.WriteLine("Correct At: " + DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            }
        }
        
        //This tests determines how long it takes for a new master to be elected once a master has died,
        //The system must wait for up to 2 Lease Periods to expire + drift + time taken to complete a round
        //of paxos.
        [TestMethod]
        public async Task MasterElectionIntegrationTest2()
        {
            DateTime Start = DateTime.UtcNow;
            Console.WriteLine("Start: " + Start.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK") + " (+0ms)");
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(20));
            Link(nodeA, nodeB, nodeC);

            //the one that goes first is always elected if everyone else waits
            Assert.AreEqual("A", await nodeA.GetMaster());
            Assert.AreEqual("A", await nodeB.GetMaster());
            Assert.AreEqual("A", await nodeC.GetMaster());

            nodeA.Connected = false;
            DateTime KillTime = DateTime.UtcNow;
            Console.WriteLine("Master Killed: " + KillTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK") + " (+" + KillTime.Subtract(Start).TotalMilliseconds + "ms)");
            var leaseExpired = false;
            string master;
            while (!leaseExpired && nodeB.TryGetMaster(out master)) await Task.Yield();
            DateTime LeaseExpireTime = DateTime.UtcNow;
            Console.WriteLine("Lease Expired: " + LeaseExpireTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK") + " (+" + LeaseExpireTime.Subtract(Start).TotalMilliseconds + "ms)");
            master = await nodeB.GetMaster();
            Console.WriteLine("New Master: " + master);
            DateTime End = DateTime.UtcNow;
            Console.WriteLine("New Master Elected: " + End.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK") + " (+" + End.Subtract(Start).TotalMilliseconds + "ms)");

        }
    }
}
