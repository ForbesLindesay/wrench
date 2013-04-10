using Paxos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph_Generator
{
    class Program
    {
        private static readonly TextWriter Error = Console.Error;
        const int NoOfIterations = 20;

        static Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> delay(double milliseconds)
        {
            return (input) => input.Pipe(new Delay<NetworkMessage>(TimeSpan.FromMilliseconds(milliseconds)));
        }
        static Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> delay(double millisecondsMin, double millisecondsMax)
        {
            return (input) => input.Pipe(new Delay<NetworkMessage>(TimeSpan.FromMilliseconds(millisecondsMin), TimeSpan.FromMilliseconds(millisecondsMax)));
        }

        static Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> drop(double percentage)
        {
            return (input) => input.Pipe(new Drop<NetworkMessage>(percentage));
        }

        static Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> and(Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> a, Func<IReadStream<NetworkMessage>, IReadStream<NetworkMessage>> b)
        {
            return (input) => b(a(input));
        }

        static void Main(string[] args)
        {
            //QueueResults("Linear Clients", Enumerable.Range(1, 10).Select((i) => testRun(i, i, i, 100, 100, 0)));
            QueueResults("Linear delay", Enumerable.Range(1, 3).SelectMany((n) => Enumerable.Range(1, 4).Select((i) => testRun(n, n, n, 100, 1000 * i, 0))));
            //QueueResults("Linear Drop", Enumerable.Range(3, 4).SelectMany((n) => Enumerable.Range(0, 8).Select((i) => testRun(n, n, n, 10, 10, ((double)i) / 20D))));
            //QueueResults("Dead Proposers", Enumerable.Range(1, 5).Select((i) => testRun(2, 10, 10-i, 100, 100, 0)));
            WriteQueue();
            Console.Error.WriteLine("DONE");
            //Console.ReadLine();
        }

        static List<KeyValuePair<string, List<Task<string>>>> queued = new List<KeyValuePair<string,List<Task<string>>>>();
        static void QueueResults(string name, IEnumerable<Task<string>> results)
        {
            queued.Add(new KeyValuePair<string, List<Task<string>>>(name, results.ToList()));
        }
        static void WriteQueue()
        {
            foreach (var result in queued)
            {
                Console.WriteLine("\"" + result.Key + "\"");
                WriteResults(result.Value);
            }
        }
        static void WriteResults(List<Task<string>> results)
        {
            foreach (var result in results.ToList())
            {
                Console.WriteLine(result.Result);
            }
        }

        static Task<string> testRun(int proposers, int acceptors, int liveAcceptors, double MinDelay, double MaxDelay, double Drop)
        {
            return new TestRun(proposers, acceptors, liveAcceptors, MinDelay, MaxDelay, Drop).Results();
        }
        class TestRun
        {
            public TestRun()
            {
            }
            public TestRun(int proposers, int acceptors, int liveAcceptors, double MinDelay, double MaxDelay, double Drop)
            {
                Proposers = proposers;
                Acceptors = acceptors;
                LiveAcceptors = liveAcceptors;
                minDelay = MinDelay;
                maxDelay = MaxDelay;
                drop = Drop;
            }

            public int Proposers;
            public int Acceptors;
            public int LiveAcceptors;
            public double minDelay;
            public double maxDelay;
            public double drop;

            public async Task<TimeSpan> Run()
            {
                DateTime start = DateTime.Now;

                for (var n = 0; n < NoOfIterations; n++)
                {
                    var proposers = new ConcurrentBag<Proposer>();
                    var acceptors = new ConcurrentBag<Acceptor>();

                    var proposedValues = new ConcurrentBag<string>();
                    var acceptedValues = new ConcurrentBag<Task<string>>();

                    for (int i = 0; i < Proposers; i++)
                    {
                        proposers.Add(new Proposer(Guid.NewGuid().ToString(), Acceptors));
                    }
                    for (int i = 0; i < LiveAcceptors; i++)
                    {
                        acceptors.Add(new Acceptor("Address"));
                    }

                    foreach (var proposer in proposers)
                        foreach (var acceptor in acceptors)
                            proposer.Pipe(new Delay<NetworkMessage>(TimeSpan.FromMilliseconds(minDelay), TimeSpan.FromMilliseconds(maxDelay)))
                                .Pipe(new Drop<NetworkMessage>(drop))
                                .Pipe(acceptor)
                                .Pipe(new Delay<NetworkMessage>(TimeSpan.FromMilliseconds(minDelay), TimeSpan.FromMilliseconds(maxDelay)))
                                .Pipe(new Drop<NetworkMessage>(drop))
                                .Pipe(proposer);

                    foreach (var proposer in proposers)
                    {

                        acceptedValues.Add(Task.Factory.StartNew(() =>
                        {
                            var val = Guid.NewGuid().ToString();
                            proposedValues.Add(val);
                            return proposer.Propose(val);
                        })
                        .Unwrap());
                    }
                    var acceptedValue = await acceptedValues.First();
                    foreach (var res in acceptedValues)
                    {
                        var result = await res;
                        if (result != acceptedValue) throw new Exception("The proposers did not all get the same result");
                        if (!proposedValues.Contains(result)) throw new Exception("The accepted Value was never proposed");
                    }
                }

                DateTime end = DateTime.Now;

                return end.Subtract(start);
            }

            public async Task<string> Results()
            {
                string time;
                try
                {
                    time = ((await Task.Factory.StartNew(() => Run()).Unwrap().Timeout(120000 * NoOfIterations)).TotalMilliseconds / NoOfIterations).ToString();
                }
                catch (TimeoutException)
                {
                    time = "Operation Timed Out";
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Error.WriteLine(e.Message);
                        Error.WriteLine(e.StackTrace);
                    }
                    time = "Exception";
                }
                return ("\"" + Proposers + "\",\"" + Acceptors + "\",\"" + LiveAcceptors + "\",\"" +
                    minDelay + "\",\"" + maxDelay + "\",\"" + (drop * 100) + "\",\"" + time + "\"");
            }
        }
    }
}
