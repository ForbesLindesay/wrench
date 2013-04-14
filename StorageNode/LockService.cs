using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageNode
{
    class LockService
    {
        private bool IsMaster()
        {
            return true;
        }

        public async Task<ILock> GetLock(string Source, string Key, TimeSpan Expiry)
        {
            if (IsMaster())
            {
                return new LocalLock(this, Source, Key);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public async void ReleaseLock(string Source, string Key)
        {
            if (IsMaster())
            {

            }
        }

        private class LocalLock : ILock
        {
            private readonly LockService service;
            private readonly string source;
            private readonly string key;
            public LocalLock(LockService Service, string Source, string Key)
            {
                service = Service;
                source = Source;
                key = Key;
            }



            public string Source
            {
                get { return source; }
            }

            public string Key
            {
                get { return key; }
            }

            public bool Expired
            {
                get { return service.IsMaster(); }
            }

            public void Dispose()
            {
                service.ReleaseLock(Source, Key);
            }
        }
    }

    interface ILock : IDisposable
    {
        string Source { get; }
        string Key { get; }
        bool Expired { get; }
    }
}
