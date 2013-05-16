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
                    n.Message += async (s, m) =>
                    {
                        //await Task.Delay(1);
                        node.OnMessage(m);
                    };
                    node.Message += async (s, m) =>
                    {
                        //await Task.Delay(1);
                        n.OnMessage(m);
                    };
                }
                nodes.Add(node);
            }

            return async (s) =>
            {
                await Task.Delay(1);
                var res = await nodes[rnd.Next(size)].Handle(s);
                await Task.Delay(1);
                return res;
            };
        }


        [TestMethod]
        public async Task WitePerf()
        {
            var ops = 100;
            var client = new Client(Cluster(5));
            using (var t1 = client.BeginWriteTransaction())
            {
                await t1.Write("foo", "bar");
                await t1.Write("bing", "bong");
                await t1.Commit();
            }
            var start = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            for (int i = 0; i < ops; i++)
            {
                using (var t1 = client.BeginWriteTransaction())
                {
                    var r1 = t1.Read("foo");
                    var r2 = t1.Read("bing");
                    var foo = await r1;
                    var bing = await r2;
                    await t1.Write("foo", bing);
                    await t1.Write("bing", foo);
                    await t1.Commit();
                }
            }
            var end = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            var opspersec = ops / (end.Subtract(start)).TotalSeconds;
            Console.WriteLine(opspersec);
        }
        [TestMethod]
        public async Task ReadPerf()
        {
            var ops = 1000;
            var cluster = Cluster(5);
            var client = new Client(cluster);
            using (var t1 = client.BeginWriteTransaction())
            {
                await t1.Write("foo", "bar");
                await t1.Write("bing", "bong");
                await t1.Write("a", "z");
                await t1.Write("b", "x");
                await t1.Commit();
            }

            var start = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            var clients = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                clients.Add(ReadN(cluster, 100));
            }
            foreach (var cl in clients)
            {
                await cl;
            }
            var end = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            var opspersec = ops / (end.Subtract(start)).TotalSeconds;
            Console.WriteLine(opspersec);
        }

        public Task ReadN(Func<string, Task<string>> Cluster, int Ops)
        {
            var client = new Client(Cluster);
            return Task.Run(async () =>
            {
                for (int i = 0; i < Ops; i++)
                {
                    using (var t1 = client.BeginWriteTransaction())
                    {
                        var r1 = t1.Read("foo");
                        var r2 = t1.Read("bing");
                        var r3 = t1.Read("a");
                        var r4 = t1.Read("b");
                        Assert.AreEqual("bar", await r1);
                        Assert.AreEqual("bong", await r2);
                        Assert.AreEqual("z", await r3);
                        Assert.AreEqual("x", await r4);
                        await t1.Commit();
                    }
                }
                });
        }

        [TestMethod]
        public async Task RacePerf()
        {
            var start = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));

            var cluster = Cluster(5);
            var client = new Client(cluster);
            await client.Write("shared", "0");
            await client.Write("p1", "0");
            await client.Write("p2", "0");
            var r1 = Race(cluster, "shared", "p1");
            var r2 = Race(cluster, "shared", "p2");

            var res1 = await r1;
            var res2 = await r2;

            var end = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"));
            var opspersec = 20 / (end.Subtract(start)).TotalSeconds;
            Console.WriteLine(opspersec);
            Console.WriteLine(res1 + "/" + res2);
        }
        public async Task<long> Race(Func<string, Task<string>> Cluster, string Shared, string Private)
        {
            var client = new Client(Cluster);
            while (true)
            {
                using (var t = client.BeginWriteTransaction())
                {
                    var s = int.Parse(await t.Read(Shared));
                    if (s >= 20) return int.Parse(await t.Read(Private));
                    var p = int.Parse(await t.Read(Private));
                    await t.Write(Shared, (s + 1).ToString());
                    await t.Write(Private, (p + 1).ToString());
                    try
                    {
                        await t.Commit();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
