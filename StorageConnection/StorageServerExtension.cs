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
                case NetworkRequest.BeginTransaction:
                    return Node.BeginTransaction().Then(n => n.ToString());
                case NetworkRequest.Read:
                    return Node.Read(req.transactionID, req.key).Then(JSON.Serialize);
                case NetworkRequest.Commit:
                    return Node.Commit(req.transactionID, req.updated, req.read).Then((n) => n.ToString());
                default:
                    throw new NotImplementedException("The method " + req.method + " was not recognised.");
            }
        }
    }
}
