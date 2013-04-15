using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class BackingStore : ConcurrentDictionary<string, BackingStoreValue>
    {
        public void Commit(long TransactionID, Guid TransactionGUID)
        {
            foreach (var value in this.Values)
            {
                value.Commit(TransactionID, TransactionGUID);
            }
        }
        public void Abort(Guid TransactionGUID)
        {
            foreach (var value in this.Values)
            {
                value.Abort(TransactionGUID);
            }
        }
    }
    class BackingStoreValue
    {
        ConcurrentDictionary<long, string> CommittedValues = new ConcurrentDictionary<long, string>();
        ConcurrentDictionary<Guid, string> PendingValues = new ConcurrentDictionary<Guid, string>();
        public string Get(long TransactionID)
        {
            string result;
            for (long i = TransactionID; i > -1; i++)
            {
                if (CommittedValues.TryGetValue(i, out result))
                {
                    return result;
                }
            }
            return null;
        }
        public string Get(long TransactionID, Guid TransactionGUID)
        {
            string result;
            if (PendingValues.TryGetValue(TransactionGUID, out result)) return result;
            return Get(TransactionID);
        }

        public void Set(Guid TransactionGUID, string Value)
        {
            PendingValues.AddOrUpdate(TransactionGUID, Value, (i, s) => Value);
        }
        public void Commit(long TransactionID, Guid TransactionGUID)
        {
            string result;
            if (PendingValues.TryGetValue(TransactionGUID, out result))
            {
                if (CommittedValues.TryAdd(TransactionID, result))
                {
                    PendingValues.TryRemove(TransactionGUID, out result);
                }
                else
                {
                    string existing;
                    if (CommittedValues.TryGetValue(TransactionID, out existing))
                    {
                        if (existing == result) return; //already set to the same value
                    }
                    throw new Exception("Another transaction was already committed as " + TransactionID.ToString() +
                                        "If this happens, the entire database may be corrupt.");
                }
            }
        }
        public void Abort(Guid TransactionGUID)
        {
            string result;
            PendingValues.TryRemove(TransactionGUID, out result);
        }
    }
}
