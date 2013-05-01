using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class BackingStore : ConcurrentDictionary<string, ConcurrentDictionary<long, string>>
    {
        public string Get(long SequenceNumber, string Key)
        {
            var store = this.GetOrAdd(Key, new ConcurrentDictionary<long, string>());
            string value;
            if (store.TryGetValue(SequenceNumber, out value)) return value;
            else return null;
        }
        public void Set(long SequenceNumber, string Key, string Value)
        {
            var store = this.GetOrAdd(Key, new ConcurrentDictionary<long, string>());
            store.TryAdd(SequenceNumber, Value);
        }
    }
}
