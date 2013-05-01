using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class AsyncSet<T>
    {
        private readonly AsyncCounter size = new AsyncCounter(0);
        private readonly ConcurrentDictionary<T, bool> Items = new ConcurrentDictionary<T, bool>();

        public bool Add(T Item)
        {
            if (Items.TryAdd(Item, true))
            {
                size.Increment();
                return true;
            }
            return false;
        }
        public bool Contains(T item)
        {
            bool res;
            if (Items.TryGetValue(item, out res)) return res;
            else return false;
        }
        public Task Wait(long Value)
        {
            return size.Wait(Value);
        }
        public long Size()
        {
            return Items.Count;
        }
    }
}
