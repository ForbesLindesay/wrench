using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Paxos.test
{
    [TestClass]
    public class SequenceNumberTests
    {
        [TestMethod]
        public void TestSequenceNumbers()
        {
            var def = new SequenceNumber();
            var small = new SequenceNumber(1, "B");
            var big = new SequenceNumber(1000, "A");
            var bigger = new SequenceNumber(1000, "B");

            Assert.IsTrue(bigger > big);
            Assert.IsTrue(big < bigger);
            Assert.IsTrue(bigger > small);
            Assert.IsTrue(small < bigger);
            Assert.IsTrue(bigger > def);
            Assert.IsTrue(def < bigger);

            Assert.IsTrue(big > small);
            Assert.IsTrue(small < big);
            Assert.IsTrue(big > def);
            Assert.IsTrue(def < big);

            Assert.IsTrue(small > def);
            Assert.IsTrue(def < small);

            Assert.AreEqual("[1:B]", small.ToString(), "Should have a neat string representation");
            Assert.AreEqual(small, new SequenceNumber("[1:B]"), "Should be able to parse string representation");

            Assert.AreNotEqual(new SequenceNumber(), null);
            Assert.AreNotEqual(null, new SequenceNumber());
            Assert.AreNotEqual(new SequenceNumber(), new Object());
            Assert.AreNotEqual(new Object(), new SequenceNumber());

            Assert.AreEqual(small.GetHashCode(), new SequenceNumber(1, "B").GetHashCode());

            Assert.IsTrue(small != big);

            Assert.IsTrue(small <= big);
            Assert.IsTrue(small <= new SequenceNumber(1, "B"));
            Assert.IsFalse(big <= small);

            Assert.IsTrue(big >= small);
            Assert.IsTrue(small >= new SequenceNumber(1, "B"));
            Assert.IsFalse(small >= big);
        }

    }
}
