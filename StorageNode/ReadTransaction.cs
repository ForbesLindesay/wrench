using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    public class ReadTransaction : IDisposable
    {
        private bool disposed = false;
        private readonly StorageNode node;
        private readonly DateTime time;

        internal ReadTransaction(StorageNode Node, DateTime Time)
        {
            node = Node;
            time = Time;
        }

        public string Get(string key)
        {
            return "";
        }

        public void Dispose()
        {
            disposed = true;
        }
    }
}
