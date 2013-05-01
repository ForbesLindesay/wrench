using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public static class StorageConnectionExtensions
    {
        public static Task<string> Read(this IStorageNode Node, Task<ReadID> ReadTransactionID, string Key)
        {
            return ReadTransactionID
                .Then((ReadID) => Node.Read(ReadID, Key))
                .Unwrap();
        }
        public static Task<string> Read(this IStorageNode Node, Task<WriteID> WriteTransactionID, string Key)
        {
            return WriteTransactionID
                .Then((WriteID) => Node.Read(WriteID, Key))
                .Unwrap();
        }
        public static Task Commit(this IStorageNode Node, Task<WriteID> WriteTransactionID, Dictionary<string, string> Updated)
        {
            return WriteTransactionID
                .Then((WriteID) => Node.Commit(WriteID, Updated))
                .Unwrap();
        }
        public static Task Abort(this IStorageNode Node, Task<WriteID> WriteTransactionID)
        {
            return WriteTransactionID
                .Then((WriteID) => Node.Abort(WriteID))
                .Unwrap();
        }
    }
}
