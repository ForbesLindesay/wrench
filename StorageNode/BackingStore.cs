using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class BackingStore : ConcurrentDictionary<string, BackingStoreValue<string>>
    {
        public string Get(long SequenceNumber, string Key)
        {
            var store = this.GetOrAdd(Key, new BackingStoreValue<string>());
            return store.Get(SequenceNumber);
        }
        public void Set(long SequenceNumber, string Key, string Value)
        {
            var store = this.GetOrAdd(Key, new BackingStoreValue<string>());
            store.Set(SequenceNumber, Value);
        }
    }
    class BackingStoreValue<T>
    {
        ConcurrentDictionary<long, T> history = new ConcurrentDictionary<long, T>();
        long latest = -1;

        public T Get(long SequenceNumber)
        {
            if (latest == -1) return default(T);
            T Value;
            if (SequenceNumber >= latest)
            {
                if (history.TryGetValue(latest, out Value)) return Value;
            }
            for (long i = SequenceNumber; i >= 0; i--)
            {
                if (history.TryGetValue(i, out Value)) return Value;
            }
            return default(T);
        }
        public void Set(long SequenceNumber, T Value)
        {
            latest = SequenceNumber;
            history.TryAdd(SequenceNumber, Value);
        }
    }
}
