using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class AsyncCounter
    {
        private long value;
        private readonly Object locker = new Object();
        private readonly HashSet<long> AutoIncrements = new HashSet<long>();
        private ConcurrentDictionary<long, TaskCompletionSource<Object>> waiters = new ConcurrentDictionary<long, TaskCompletionSource<object>>();

        public AsyncCounter() : this(0) { }
        public AsyncCounter(long InitialValue)
        {
            value = InitialValue;
        }

        private TaskCompletionSource<Object> Store(long Value)
        {
            return waiters.GetOrAdd(Value, k => new TaskCompletionSource<Object>());
        }

        public Task Wait(long Value)
        {
            lock (locker)
            {
                if (value >= Value)
                {
                    return Task.FromResult<Object>(null);
                }
                else
                {
                    return Store(Value).Task;
                }
            }
        }
        private void increment()
        {
            value++;
            AutoIncrements.Remove(value);
            TaskCompletionSource<Object> completion;
            if (waiters.TryGetValue(value, out completion))
            {
                completion.TrySetResult(null);
                waiters.TryRemove(value, out completion);
            }
            if (AutoIncrements.Contains(value + 1))
            {
                increment();
            }
        }
        public long Increment()
        {
            lock (locker)
            {
                increment();
                return value;
            }
        }
        public long Increment(long To)
        {
            lock (locker)
            {
                while (value < To) increment();
                return value;
            }
        }

        public void AutoIncrementOn(long Value)
        {
            lock (locker)
            {
                if (value == Value - 1)
                {
                    increment();
                }
                else
                {
                    AutoIncrements.Add(Value);
                }
            }
        }

        public long Current()
        {
            lock (locker)
            {
                return value;
            }
        }
    }
}
