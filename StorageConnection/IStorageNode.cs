using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageConnection
{
    public interface IStorageNode
    {
        Task<ReadID> BeginReadTransaction();
        Task<string> Read(ReadID ReadTransactionID, string Key);

        Task<WriteID> BeginWriteTransaction(string[] Keys);
        Task Commit(WriteID WriteTransactionID, Dictionary<string, string> Updated, string[] Read);
        Task Abort(WriteID WriteTransactionID);
    }
}
