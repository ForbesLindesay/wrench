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
        public ReadID readTransactionID;
        public WriteID writeTransactionID;
        public string key;
        public string[] keys;
        public Dictionary<string, string> updated;


        public const string BeginReadTransaction = "BeginReadTransaction";
        public const string Read = "Read";
        public const string BeginWriteTransaction = "BeginWriteTransaction";
        public const string Commit = "Commit";
        public const string Abort = "Abort";

    }
}
