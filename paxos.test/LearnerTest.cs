using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos.test
{
    [TestClass]
    public class LearnerTest
    {
        [TestMethod]
        public async Task LearnerLearnsOfResult()
        {
            string result;
            var learner = new Learner(3);
            Assert.IsFalse(learner.TryGetResult("round", out result));
            learner.SendMessage("", "AcceptorA", NetworkMessage.Commit("round", new SequenceNumber("[1:ProposerA]"), "foo").Accept());
            learner.SendMessage("", "AcceptorA", NetworkMessage.Commit("round", new SequenceNumber("[1:ProposerA]"), "foo").Accept());
            learner.SendMessage("", "AcceptorA", NetworkMessage.Commit("round", new SequenceNumber("[1:ProposerA]"), "foo").Accept());
            Assert.IsFalse(learner.TryGetResult("round", out result));
            learner.SendMessage("", "AcceptorB", NetworkMessage.Commit("round", new SequenceNumber("[2:ProposerB]"), "bar").Accept());
            learner.SendMessage("", "AcceptorC", NetworkMessage.Commit("round", new SequenceNumber("[2:ProposerB]"), "bar").Accept());

            Assert.AreEqual("bar", await learner.GetResult("round"));
            Assert.IsTrue(learner.TryGetResult("round", out result));
            Assert.AreEqual("bar", result);
        }
    }
}
