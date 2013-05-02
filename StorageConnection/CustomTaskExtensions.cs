using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public static class CustomTaskExtensions
    {
        public static Task<S> Then<T, S>(this Task<T> Tsk, Func<T, S> OnFulfilled)
        {
            return Tsk.ContinueWith((t) => 
                {
                    if (!(t.IsCanceled || t.IsFaulted))
                        return Task.FromResult(OnFulfilled(t.Result));
                    else
                        return From<S>(t);
                })
                .Unwrap();
        }
        public static Task<T> Then<T>(this Task Task, Func<T> OnFulfilled)
        {
            return Task.ContinueWith((t) =>
                {
                    if (!(t.IsCanceled || t.IsFaulted))
                        return Task.FromResult(OnFulfilled());
                    else
                        return From<T>(t);
                })
                .Unwrap();
        }
        private static Task<S> From<S>(Task Tsk)
        {
            var tcs = new TaskCompletionSource<S>();
            if (Tsk.IsCanceled)
            {
                tcs.SetCanceled();
            }
            else if (Tsk.IsFaulted)
            {
                tcs.SetException(Tsk.Exception);
            }
            else
            {
                throw new Exception("Can only convert when the task is faulted or cancelled");
            }
            return tcs.Task;
        }
    }
}
