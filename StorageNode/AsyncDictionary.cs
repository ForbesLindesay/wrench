using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class AsyncDictionary<T, S>
    {
        private readonly ConcurrentDictionary<T, TaskCompletionSource<S>> store = new ConcurrentDictionary<T, TaskCompletionSource<S>>();

        private TaskCompletionSource<S> Store(T Key)
        {
            return store.GetOrAdd(Key, k => new TaskCompletionSource<S>());
        }

        public bool TryGet(T Key, out S Value)
        {
            var result = Store(Key).Task;
            if (result.IsCompleted)
            {
                Value = result.Result;
                return true;
            }
            else
            {
                Value = default(S);
                return false;
            }
        }

        public Task<S> Get(T Key)
        {
            var result = Store(Key).Task;
            if (result.IsCanceled || result.IsCompleted || result.IsFaulted)
            {
                return result;
            }
            else
            {
                var handler = KeyRequested;
                if (handler != null)
                {
                    handler(this, Key);
                }
                return result;
            }
        }
        public bool TrySet(T Key, S Value)
        {
            return Store(Key).TrySetResult(Value);
        }
        public bool TryReject(T Key, Exception Exception)
        {
            return Store(Key).TrySetException(Exception);
        }
        public bool TryCancel(T Key)
        {
            return Store(Key).TrySetCanceled();
        }

        public void Dispose(T Key)
        {
            TaskCompletionSource<S> value;
            store.TryRemove(Key, out value);
        }

        public event EventHandler<T> KeyRequested;
    }
}
