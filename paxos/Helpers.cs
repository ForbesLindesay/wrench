using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos
{
    public static class Helpers
    {
        public static Task<T> Timeout<T>(this Task<T> task, int ms)
        {
            return Task.WhenAny(task, Task.Delay(ms))
                .ContinueWith<T>((t) =>
                {
                    if (!task.IsCompleted && t != task)
                    {
                        throw new TimeoutException("The task timed out");
                    }

                    return task.Result;
                });
        }
    }
}
