using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos.test
{
    [TestClass]
    public class PaxosNodeTest
    {
        [TestMethod]
        public async Task PaxosNodesPassResults()
        {
            var A = new PaxosNode("A", 3);
            var B = new PaxosNode("B", 3);
            var C = new PaxosNode("C", 3);
            Group(A, B, C);

            string result;

            Assert.IsFalse(A.TryGetResult("round", out result));
            Assert.IsFalse(B.TryGetResult("round", out result));
            Assert.IsFalse(C.TryGetResult("round", out result));

            var t1 = A.Propose("round", "foo");
            var expected = await t1;
            var t2 = B.Propose("round", "bar");
            Assert.IsTrue(expected == "foo" || expected == "bar");
            Assert.AreEqual(expected, await t2);

            Assert.AreEqual(expected, await B.GetResult("round"));
            Assert.AreEqual(expected, await A.GetResult("round"));
            Assert.AreEqual(expected, await C.GetResult("round"));
        }

        private void Group(params PaxosNode[] Nodes)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                var x = i;
                Nodes[x].Message += (s, message) =>
                {
                    for (int y = 0; y < Nodes.Length; y++)
                    {
                        if (y != x) Nodes[y].OnMessage(message);
                    }
                };
            }
        }
    }
}
