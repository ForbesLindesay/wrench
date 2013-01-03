using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryStore
{
    public class Store
    {
        private readonly Dictionary<string, SortedDictionary<DateTime, string>> internalStore = new Dictionary<string, SortedDictionary<DateTime, string>>();
        public string Get(string key)
        {
            var time = DateTime.Now;
            return internalStore[key].Last((pair) => pair.Key <= time).Value;
        }
        public string Get(string key, DateTime time)
        {
            return internalStore[key].Last((pair) => pair.Key < time).Value;
        }
        public void Set(string key, string value)
        {
            if (!internalStore.ContainsKey(key)) internalStore[key] = new SortedDictionary<DateTime,string>();
            internalStore[key][DateTime.Now] = value;
        }
        public void Set(string key, string value, DateTime time)
        {
            if (!internalStore.ContainsKey(key)) internalStore[key] = new SortedDictionary<DateTime, string>();
            internalStore[key][time] = value;
        }
    }
}
