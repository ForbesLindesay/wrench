using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Paxos
{
    class Learner:IWriteStream<NetworkMessage>
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>> respones
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>>();
        private readonly ConcurrentDictionary<string, string> results = new ConcurrentDictionary<string,string>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Action<string>>> handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<string>>>();

        public int TotalAcceptors { get; set; }
        public int Quorum
        {
            get { return (TotalAcceptors / 2) + 1; }
        }
        public Learner(int TotalAcceptors)
        {
            this.TotalAcceptors = TotalAcceptors;
        }

        public bool TryGetResult(string RoundID, out string Result)
        {
            return results.TryGetValue(RoundID, out Result);
        }
        public Task<string> GetResult(string RoundID)
        {
            var tcs = new TaskCompletionSource<string>();
            var evt = handlers.GetOrAdd(RoundID, new ConcurrentBag<Action<string>>());
            evt.Add((Result) => tcs.TrySetResult(Result));
            string res;
            if (results.TryGetValue(RoundID, out res))
            {
                tcs.TrySetResult(res);
                handlers.TryRemove(RoundID, out evt);
            }
            return tcs.Task;
        }

        public void SendMessage(string addressTo, string addressFrom, NetworkMessage message)
        {
            if (!results.ContainsKey(message.RoundID))
            {
                var attempt = respones.GetOrAdd(message.RoundID, new ConcurrentDictionary<string, ConcurrentDictionary<string, bool>>());
                var responses = attempt.GetOrAdd(message.Value, new ConcurrentDictionary<string, bool>());
                if (responses.TryAdd(addressFrom, true))
                {
                    var count = responses.Count;
                    if (count > Quorum)
                    {
                        if (results.TryAdd(message.RoundID, message.Value))
                        {
                            var handler = RoundResult;
                            if (handler != null)
                            {
                                handler(this, new RoundResult(message.RoundID, message.Value));
                            }
                            var evt = handlers.GetOrAdd(message.RoundID, new ConcurrentBag<Action<string>>());
                            foreach (var h in evt)
                            {
                                h(message.Value);
                            }
                            handlers.TryRemove(message.RoundID, out evt);
                        }
                    }
                }
                if (results.ContainsKey(message.RoundID))
                {
                    ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> res;
                    respones.TryRemove(message.RoundID, out res);
                }
            }
        }

        public event EventHandler<RoundResult> RoundResult;
    }
    public class RoundResult
    {
        public RoundResult(string RoundID, string Result)
        {
            this.RoundID = RoundID;
            this.Result = Result;
        }

        public readonly string RoundID;
        public readonly string Result;
    }
}
