﻿using StorageConnection;
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

        public ReadTransaction(IStorageNode Node)
        {
            node = Node;
            id = Node.BeginTransaction();
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
