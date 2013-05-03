using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public static class StorageConnectionExtensions
    {
        public static Task<string> Read(this IStorageNode Node, Task<long> TransactionID, string Key)
        {
            return TransactionID
                .Then((ReadID) => Node.Read(ReadID, Key))
                .Unwrap();
        }
        public static Task<long> Commit(this IStorageNode Node, Task<long> TransactionID, Dictionary<string, string> Updated, string[] Read)
        {
            return TransactionID
                .Then((WriteID) => Node.Commit(WriteID, Updated, Read))
                .Unwrap();
        }
    }
}
