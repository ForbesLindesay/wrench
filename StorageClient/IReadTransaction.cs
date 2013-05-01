using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    public interface IReadTransaction : IDisposable
    {
        Task<string> Read(string Key);
    }
}
