using StorageConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    class ReadTransaction : IReadTransaction
    {
        private readonly IStorageNode node;
        private readonly Task<long> id;

        public ReadTransaction(IStorageNode Node, long MinSequenceNumber, Action<long> UpdateSequenceNumber)
        {
            node = Node;
            id = Node.BeginTransaction()
                 .Then(n =>
                 {
                     if (n > MinSequenceNumber) UpdateSequenceNumber(n);
                     return Math.Max(n, MinSequenceNumber);
                 });
        }

        public Task<string> Read(string Key)
        {
            return node.Read(id, Key);
        }

        public void Dispose()
        {
            //no-op
        }
    }
}
