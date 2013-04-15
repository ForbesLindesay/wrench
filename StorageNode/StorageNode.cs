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
        private readonly Master master;

        private long lastCompletedTransaction = -1;
        public long LastCompletedTransaction { get { return lastCompletedTransaction; } }



        internal async Task<string> Read(long TransactionID, string Key)
        {
            if (TransactionID > LastCompletedTransaction)
            {
                await WaitForCompletion(TransactionID);
            }
            //todo: Do Read
            throw new NotImplementedException();
        }

        internal async Task Write(Guid TransactionID, string Key, string Value)
        {
            //todo: Do Write
            throw new NotImplementedException();
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
