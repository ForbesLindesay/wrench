using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using StorageConnection;
using StorageNode;
using StorageClient;

namespace StorageTest
{
    [TestClass]
    public class Integration
    {
        [TestMethod]
        public async Task NonConflict()
        {
            var client = new Client(Cluster(5));
            using (var t = client.BeginWriteTransaction())
            {
                await t.Write("foo", "bing");
                await t.Write("bar", "bing");
                await t.Commit();
            }
            await Task.Delay(500);//allow for propogatation
            using (var t = client.BeginReadTransaction())
            {
                Assert.AreEqual("bing", await t.Read("foo"));
                using (var w = client.BeginWriteTransaction())
                {
                    await w.Write("foo", "baz");
                    await w.Write("bar", "baz");
                    await w.Commit();
                    await Task.Delay(500);//allow for propogation
                }
                //note that read remains consistent despite interleaving write
                Assert.AreEqual("bing", await t.Read("bar"));
            }
        }
        [TestMethod]
        public async Task Conflict()
        {
            var client = new Client(Cluster(5));
            var aborted = false;
            using (var t1 = client.BeginWriteTransaction())
            {
                using (var t2 = client.BeginWriteTransaction())
                {
                    await t1.Write("foo", "bar");
                    await t2.Write("foo", await t2.Read("foo") + "baz");
                    await t1.Commit();
                    try
                    {
                        await t2.Commit();
                    }
                    catch (Exception)
                    {
                        aborted = true;
                    }
                }
            }
            await Task.Delay(500);//allow for propogatation
            using (var t = client.BeginReadTransaction())
            {
                Assert.IsTrue(aborted);
                Assert.AreEqual("bar", await t.Read("foo"));
            }
        }

        private Func<string, Task<string>> Cluster(int size)
        {
            var rnd = new Random();
            var nodes = new List<StorageNode.StorageNode>();
            for (int i = 0; i < size; i++)
            {
                var node = new StorageNode.StorageNode(size);
                foreach (var n in nodes)
                {
                    n.Message += (s, m) => node.OnMessage(m);
                    node.Message += (s, m) => n.OnMessage(m);
                }
                nodes.Add(node);
            }
            
            return (s) => nodes[rnd.Next(size)].Handle(s);
        }
    }
}
