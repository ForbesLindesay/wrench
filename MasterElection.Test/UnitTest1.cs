using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MasterElection.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task SingleNodeIsMaster()
        {
            var me = new Master("address", 1);
            await Task.Delay(1000);
            Assert.IsTrue(me.IsMaster);
        }
    }
}
