using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StorageConnection
{
    public class NetworkRequest
    {
        public string method;
        public long transactionID;
        public string key;
        public string[] keys;
        public Dictionary<string, string> updated;
        public string[] read;


        public const string BeginTransaction = "BeginTransaction";
        public const string Read = "Read";
        public const string Commit = "Commit";

    }
}
