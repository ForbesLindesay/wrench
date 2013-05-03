using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public interface IStorageNode
    {
        Task<long> BeginTransaction();
        Task<string> Read(long TransactionID, string Key);

        Task<long> Commit(long TransactionID, Dictionary<string, string> Updated, string[] Read);
    }
}
