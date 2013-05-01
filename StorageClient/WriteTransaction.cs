using StorageConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    class WriteTransaction : IWriteTransaction
    {
        private bool settled = false;
        private readonly IStorageNode node;
        private readonly Task<WriteID> id;
        private readonly HashSet<string> keys;
        private readonly Dictionary<string, string> updated = new Dictionary<string, string>();

        public WriteTransaction(IStorageNode Node, string[] Keys)
        {
            node = Node;
            id = node.BeginWriteTransaction(Keys);
            keys = new HashSet<string>(Keys);
        }

        public async Task<string> Read(string Key)
        {
            if (!keys.Contains(Key)) throw new KeyNotInTransactionException(Key);
            if (updated.ContainsKey(Key))
            {
                return updated[Key];
            }
            return await node.Read(id, Key);
        }

        public async Task Write(string Key, string Value)
        {
            if (!keys.Contains(Key)) throw new KeyNotInTransactionException(Key);
            updated[Key] = Value;
            await Task.Yield();
        }


        public Task Commit()
        {
            settled = true;
            return node.Commit(id, updated);
        }

        public Task Abort()
        {
            settled = true;
            return node.Abort(id);
        }

        public void Dispose()
        {
            if (!settled) Abort().Wait();
        }
    }

    public class KeyNotInTransactionException : Exception
    {
        internal KeyNotInTransactionException(string Key)
            : base("You attempted to access the key " + Key + " but it was not in the transaction")
        {
        }

    }
}
