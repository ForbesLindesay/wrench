using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public static class CustomTaskExtensions
    {
        public static Task<S> Then<T, S>(this Task<T> Task, Func<T, S> OnFulfilled)
        {
            return Task.ContinueWith((t) => OnFulfilled(t.Result));
        }
        public static Task<T> Then<T>(this Task Task, Func<T> OnFulfilled)
        {
            return Task.ContinueWith((t) => OnFulfilled());
        }
    }
}
