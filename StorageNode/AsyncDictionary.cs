using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private async void resolve(T Key, Task task, CancellationToken cancellation)
        {
            var delay = 10000;
            while (!(task.IsCanceled || task.IsCompleted || task.IsFaulted) && !cancellation.IsCancellationRequested)
            {
                var handler = KeyRequested;
                if (handler != null)
                {
                    handler(this, Key);
                }
                await Task.WhenAny(task, Task.Delay(delay, cancellation));
                delay = delay * 2;
            }
        }
        public Task<S> Get(T Key, CancellationToken cancellation)
        {
            var result = Store(Key).Task;
            resolve(Key, result, cancellation);
            return result;
        }
        public Task<S> Get(T Key)
        {
            return Get(Key, CancellationToken.None);
        }
        public Task<S> Get(T Key, TimeSpan Timeout)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(Timeout);
            var result = Get(Key, cts.Token);
            return Task.WhenAny(result, Task.Delay(Timeout))
                .ContinueWith((t) =>
                {
                    if (t.Result == result)
                    {
                        return result.Result;
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                });
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
            if (store.TryRemove(Key, out value))
            {
                value.TrySetCanceled();
            }
        }

        public event EventHandler<T> KeyRequested;
    }
}
