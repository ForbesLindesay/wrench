
using StorageConnection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageClient
{
    public class Client
    {
        private readonly IStorageNode node;

        public Client(Func<string, Task<string>> Request) : this(new StorageConnection.StorageClient(Request)) { }
        public Client(IStorageNode Node)
        {
            node = Node;
        }

        public Task<string> Read(string Key)
        {
            using (var transaction = BeginReadTransaction())
            {
                return transaction.Read(Key);
            }
        }
        public async Task Write(string Key, string Value)
        {
            using (var transaction = BeginWriteTransaction())
            {
                await transaction.Write(Key, Value);
                await transaction.Commit();
            }
        }
        public IReadTransaction BeginReadTransaction()
        {
            return new ReadTransaction(node);
        }
        public IWriteTransaction BeginWriteTransaction()
        {
            return new WriteTransaction(node);
        }
        public async Task Transact(Func<IWriteTransaction, Task> Fn)
        {
            var ran = false;
            while (!ran)
            {
                try
                {
                    using (var transaction = BeginWriteTransaction())
                    {
                        await Fn(transaction);
                        await transaction.Commit();
                        ran = true;
                    }
                }
                catch (Exception)
                {
                    ran = false;//run it again
                }
            }
        }
    }
}
