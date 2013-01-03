using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemoryStore.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestWithoutTimes()
        {
            var store = new MemoryStore.Store();
            store.Set("foo", "bar");
            Assert.AreEqual(store.Get("foo"), "bar");
            store.Set("foo", "baz");
            Assert.AreEqual(store.Get("foo"), "baz");
        }
        [TestMethod]
        public void TestWithTimes()
        {
            var store = new MemoryStore.Store();
            store.Set("foo", "bar", new DateTime(5000));
            Assert.AreEqual(store.Get("foo"), "bar");
            store.Set("foo", "baz", new DateTime(1000));
            Assert.AreEqual(store.Get("foo"), "bar");
            Assert.AreEqual(store.Get("foo", new DateTime(3000)), "baz");
            Assert.AreEqual(store.Get("foo", new DateTime(6000)), "bar");
        }
    }
}
