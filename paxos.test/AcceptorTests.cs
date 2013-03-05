using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos.test
{
    [TestClass]
    public class AcceptorTests
    {
        [TestMethod]
        public void AcceptsFreshProposal()
        {
            var a = new Paxos.Acceptor();
            var responseA = a.Propose("-", new SequenceNumber(1, "A"));
            var responseB = a.Propose("-", new SequenceNumber(0, "B"));
            var responseC = a.Propose("-", new SequenceNumber(2, "B"));
            var cResponseA = a.Commit("-", new SequenceNumber(1, "A"), "foo");
            var cResponseC = a.Commit("-", new SequenceNumber(2, "B"), "bar");
            var responseD = a.Propose("-", new SequenceNumber(3, "A"));
            var cResponseD = a.Commit("-", new SequenceNumber(3, "A"), "bar");

            Assert.IsTrue(responseA.IsAgreed, "First proposal should be agreed to");
            Assert.AreEqual(null, responseA.AgreedProposal, "First proposal have null as accepted proposal");

            Assert.IsFalse(responseB.IsAgreed, "It shouldn't accept lower numbered proposals");
            Assert.AreEqual(new SequenceNumber(1, "A"), responseB.HighestAgreedSequenceNumber);

            Assert.IsTrue(responseC.IsAgreed, "Higher number proposal should be agreed to");
            Assert.AreEqual(null, responseC.AgreedProposal);

            Assert.IsFalse(cResponseA.IsAccepted, "Rejects old proposal");
            Assert.IsTrue(cResponseC.IsAccepted, "Accepts fresh proposal");

            Assert.IsTrue(responseD.IsAgreed, "Higher number proposal should be agreed to");
            Assert.AreEqual("bar", responseD.AgreedProposal);

            Assert.IsTrue(cResponseD.IsAccepted, "Accepts repeat proposal");
        }
    }
}
