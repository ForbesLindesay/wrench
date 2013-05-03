using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StorageConnection
{
    public class StorageClient : IStorageNode
    {
        private static readonly JavaScriptSerializer JSON = new JavaScriptSerializer();
        private readonly Func<string, Task<string>> makeRequest;
        public StorageClient(Func<string, Task<string>> Request)
        {
            makeRequest = Request;
        }

        private Task<T> request<T>(NetworkRequest req)
        {
            return makeRequest(JSON.Serialize(req)).Then(JSON.Deserialize<T>);
        }

        public Task<long> BeginTransaction()
        {
            return request<long>(new NetworkRequest() { method = NetworkRequest.BeginTransaction });
        }

        public Task<string> Read(long ReadTransactionID, string Key)
        {
            return request<string>(new NetworkRequest()
            {
                method = NetworkRequest.Read,
                transactionID = ReadTransactionID,
                key = Key
            });
        }

        public Task<long> Commit(long WriteTransactionID, Dictionary<string, string> Updated, string[] Read)
        {
            return request<long>(new NetworkRequest()
            {
                method = NetworkRequest.Commit,
                transactionID = WriteTransactionID,
                updated = Updated,
                read = Read
            });
        }
    }
}
