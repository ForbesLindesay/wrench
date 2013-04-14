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
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(20), 1);
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(20), 1);
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(20), 1);
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

        //[TestMethod]
        public async Task MasterElectionIntegrationTest()
        {
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(20), 1);
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(20), 1);
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(20), 1);
            Link(nodeA, nodeB, nodeC);
            var A = nodeA.GetMaster();
            var B = nodeB.GetMaster();
            var C = nodeC.GetMaster();
            Assert.AreEqual(await A, await B);
            Assert.AreEqual(await B, await C);
            var master = await A;

            for (int i = 0; i < 5 * 60; i++)
            {
                await Task.Delay(1000);
                MastersLive(master, nodeA, nodeB, nodeC);
            }
        }

        //[TestMethod]
        public async Task MasterElectionIntegrationTest2()
        {
            var nodeA = new Master("A", 3, TimeSpan.FromSeconds(20), 1);
            var nodeB = new Master("B", 3, TimeSpan.FromSeconds(20), 1);
            var nodeC = new Master("C", 3, TimeSpan.FromSeconds(20), 1);
            Link(nodeA, nodeB, nodeC);

            //the one that goes first is always elected if everyone else waits
            Assert.AreEqual("A", await nodeA.GetMaster());
            Assert.AreEqual("A", await nodeB.GetMaster());
            Assert.AreEqual("A", await nodeC.GetMaster());

            nodeA.Connected = false;
            DateTime Start = DateTime.UtcNow;
            Console.WriteLine(Start.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            while (await nodeB.GetMaster() == "A") ;
            Console.WriteLine(await nodeB.GetMaster());
            DateTime End = DateTime.UtcNow;
            Console.WriteLine(End.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));

        }
    }
}
