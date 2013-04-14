using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class WriteTransaction : IDisposable
    {
        private readonly StorageNode node;
        private readonly HashSet<string> locks = new HashSet<string>();
        private Task GotLocks;

        internal WriteTransaction(StorageNode Node, IEnumerable<string> Keys)
        {
            node = Node;
            GotLocks = GetLocks(Keys);
        }
        private async Task GetLocks(IEnumerable<string> Keys)
        {
            foreach (var key in Keys.OrderBy(k => k))
            {
                locks.Add(key);
            }
        }



        public void Dispose()
        {
            //todo: roll back if not committed
            throw new NotImplementedException();
        }
    }
}
