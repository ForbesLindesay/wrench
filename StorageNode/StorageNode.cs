using MasterElection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    public class StorageNode
    {
        private readonly BackingStore store = new BackingStore();
        private readonly Master master;

        private long lastCompletedTransaction = -1;
        public long LastCompletedTransaction { get { return lastCompletedTransaction; } }



        internal async Task<string> Read(long TransactionID, string Key)
        {
            if (TransactionID > LastCompletedTransaction)
            {
                await WaitForCompletion(TransactionID);
            }
            BackingStoreValue storeValue;
            if (store.TryGetValue(Key, out storeValue))
            {
                return storeValue.Get(TransactionID);
            }
            return null;
        }
        internal async Task<string> Read(long TransactionID, Guid TransactionGUID, string Key)
        {
            if (TransactionID > LastCompletedTransaction)
            {
                await WaitForCompletion(TransactionID);
            }
            BackingStoreValue storeValue;
            if (store.TryGetValue(Key, out storeValue))
            {
                return storeValue.Get(TransactionID, TransactionGUID);
            }
            return null;
        }

        internal void Write(Guid TransactionID, string Key, string Value)
        {
            var storeValue = store.GetOrAdd(Key, new BackingStoreValue());
            storeValue.Set(TransactionID, Value);
        }

        public IReadTransaction Reader()
        {
            return new ReadTransaction(this, LastCompletedTransaction);
        }

        public void Write(Guid Transaction, string key, string value)
        {

        }

        public void Commit(Guid Transaction)
        {

        }
        private void commit(long ID, Guid Transaction)
        {
        }
        private void abort(Guid Transaction)
        {
        }

        private async Task WaitForCompletion(long TransactionID)
        {
        }

    }
}
