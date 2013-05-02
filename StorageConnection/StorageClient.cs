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

        private Task request(NetworkRequest req)
        {
            return makeRequest(JSON.Serialize(req));
        }
        private Task<T> request<T>(NetworkRequest req)
        {
            return makeRequest(JSON.Serialize(req)).Then(JSON.Deserialize<T>);
        }

        public Task<ReadID> BeginReadTransaction()
        {
            return request<ReadID>(new NetworkRequest() { method = NetworkRequest.BeginReadTransaction });
        }

        public Task<string> Read(ReadID ReadTransactionID, string Key)
        {
            return request<string>(new NetworkRequest()
            {
                method = NetworkRequest.Read,
                readTransactionID = ReadTransactionID,
                key = Key
            });
        }

        public Task<WriteID> BeginWriteTransaction(string[] Keys)
        {
            return request<WriteID>(new NetworkRequest()
            {
                method = NetworkRequest.BeginWriteTransaction,
                keys = Keys
            });
        }

        public Task Commit(WriteID WriteTransactionID, Dictionary<string, string> Updated, string[] Read)
        {
            return request(new NetworkRequest()
            {
                method = NetworkRequest.Commit,
                writeTransactionID = WriteTransactionID,
                updated = Updated,
                read = Read
            });
        }

        public Task Abort(WriteID WriteTransactionID)
        {
            return request(new NetworkRequest()
            {
                method = NetworkRequest.Abort,
                writeTransactionID = WriteTransactionID
            });
        }
    }
}
