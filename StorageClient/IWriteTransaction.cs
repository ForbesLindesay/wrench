using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    public interface IWriteTransaction : IReadTransaction
    {
        Task Write(string Key, string Value);
        Task Commit();
    }
}
