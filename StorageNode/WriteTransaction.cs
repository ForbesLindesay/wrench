using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{

    interface IWriteTransaction : IDisposable
    {
        TransactionState State { get; }
        Task<string> Read(string Key);
        Task Write(string Key, string Value);
        Task Commit();
        Task Abort();
    }
    class WriteTransaction : IWriteTransaction
    {
        private TransactionState state;
        private readonly StorageNode node;
        private readonly Guid transactionID = Guid.NewGuid();
        private readonly HashSet<string> locks = new HashSet<string>();

        internal WriteTransaction(StorageNode Node, IEnumerable<string> Keys)
        {
            node = Node;
            foreach (var key in Keys)
            {
                locks.Add(key);
            }
        }

        public TransactionState State { get { return state; } }

        public Task<string> Read(string Key)
        {
            if (State != TransactionState.Pending)
            {
                throw new Exception("You can't read from a transaction which has already been committed or aborted");
            }
            throw new NotImplementedException();
        }

        public Task Write(string Key, string Value)
        {
            if (State != TransactionState.Pending)
            {
                throw new Exception("You can't write to a transaction which has already been committed or aborted");
            }
            throw new NotImplementedException();
        }

        public async Task Commit()
        {
            if (State != TransactionState.Pending)
            {
                throw new Exception("You can't commit a transaction which has already been committed or aborted");
            }
            state = TransactionState.Committing;
            throw new NotImplementedException();
        }

        public Task Abort()
        {
            if (State != TransactionState.Pending)
            {
                throw new Exception("You can't abort a transaction which has already been committed or aborted");
            }
            state = TransactionState.Aborting;
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (State == TransactionState.Pending)
            {
                Abort();
            }
        }
    }

    public enum TransactionState
    {
        Committing,
        Committed,
        Aborting,
        Aborted,
        Pending
    }
}
