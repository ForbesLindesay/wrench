using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos
{
    public interface ITaskQueue<T>
    {
        void Enqueue(T item);
        Task<T> Dequeue(TimeSpan timeout, CancellationToken token);
    }

    /// <summary>
    /// Thread safe queue that lets you wait for an item to be available
    /// </summary>
    public class TaskQueue<T> : ITaskQueue<T>
    {
        private readonly Object locker = new Object();
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        private TaskCompletionSource<object> waiting = null;

        private async Task wait(TimeSpan timeout, CancellationToken token)
        {
            Task waiter;
            lock (locker)
            {
                if (queue.Any()) return;

                if (waiting == null) waiting = new TaskCompletionSource<object>();

                waiter = waiting.Task;
            }
            using (token.Register(() =>
            {
                lock (locker)
                {
                    if (waiting != null)
                    {
                        waiting.TrySetResult(null);
                    }
                }
            }))
            {

                await Task.WhenAny(waiter, Task.Delay(timeout));
            }
        }

        public void Enqueue(T item)
        {
            lock (locker)
            {
                queue.Enqueue(item);
                if (waiting != null)
                {
                    var waiter = waiting;
                    waiting = null;
                    waiter.SetResult(null);
                }
            }
        }

        public async Task<T> Dequeue(TimeSpan timeout, CancellationToken token)
        {
            DateTime start = DateTime.UtcNow;
            while (DateTime.UtcNow.Subtract(start) < timeout)
            {
                T result;
                if (queue.TryDequeue(out result))
                {
                    return result;
                }
                token.ThrowIfCancellationRequested();
                await wait(timeout, token);
            }
            throw new TimeoutException("Dequeing an item from the TaskQueue timed out");
        }
    }

    interface IDisposableTaskQueue<T> : ITaskQueue<T>, IDisposable
    {
    }

    public sealed class DisposableTaskQueue<T> : IDisposableTaskQueue<T>
    {
        private readonly ITaskQueue<T> innerQueue;
        private readonly Action dispose;

        public DisposableTaskQueue(ITaskQueue<T> Queue, Action Dispose)
        {
            innerQueue = Queue;
            dispose = Dispose;
        }

        public void Enqueue(T item)
        {
            innerQueue.Enqueue(item);
        }

        public Task<T> Dequeue(TimeSpan timeout, CancellationToken token)
        {
            return innerQueue.Dequeue(timeout, token);
        }

        public void Dispose()
        {
            dispose();
        }
    }
}
