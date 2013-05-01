using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StorageConnection
{
    public static class StorageServerExtension
    {
        private static readonly JavaScriptSerializer JSON = new JavaScriptSerializer();
        public static Task<string> Handle(this IStorageNode Node, string Request)
        {
            var req = JSON.Deserialize<NetworkRequest>(Request);
            switch (req.method)
            {
                case NetworkRequest.BeginReadTransaction:
                    return Node.BeginReadTransaction().Then(JSON.Serialize);
                case NetworkRequest.Read:
                    return Node.Read(req.readTransactionID, req.key).Then(JSON.Serialize);
                case NetworkRequest.BeginWriteTransaction:
                    return Node.BeginWriteTransaction(req.keys).Then(JSON.Serialize);
                case NetworkRequest.Commit:
                    return Node.Commit(req.writeTransactionID, req.updated, req.read).Then(() => "null");
                case NetworkRequest.Abort:
                    return Node.Abort(req.writeTransactionID).Then(() => "null");
                default:
                    throw new NotImplementedException("The method " + req.method + " was not recognised.");
            }
        }
    }
}
