using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    interface IReadTransaction : IDisposable
    {
        Task<string> Get(string Key);
    }
    public class ReadTransaction : IReadTransaction
    {
        private bool disposed = false;
        private readonly StorageNode node;
        private readonly long id;

        internal ReadTransaction(StorageNode Node, long ID)
        {
            node = Node;
            id = ID;
        }

        public Task<string> Get(string Key)
        {
            return node.Read(id, Key);
        }

        public void Dispose()
        {
            disposed = true;
        }
    }
}
