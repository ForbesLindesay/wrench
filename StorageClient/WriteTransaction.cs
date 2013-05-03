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
        private readonly IStorageNode node;
        private readonly Task<long> id;
        private readonly Dictionary<string, string> updated = new Dictionary<string, string>();
        private readonly HashSet<string> read = new HashSet<string>();

        public WriteTransaction(IStorageNode Node)
        {
            node = Node;
            id = node.BeginTransaction();
        }

        public async Task<string> Read(string Key)
        {
            read.Add(Key);
            if (updated.ContainsKey(Key))
            {
                return updated[Key];
            }
            return await node.Read(id, Key);
        }

        public async Task Write(string Key, string Value)
        {
            updated[Key] = Value;
            await Task.Yield();
        }


        public Task Commit()
        {
            return node.Commit(id, updated, read.ToArray());
        }

        public void Dispose()
        {
            //noop
        }
    }
}
