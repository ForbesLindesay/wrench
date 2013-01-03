using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos.test
{

    public interface IRandomStream<T> : IReadStream<T>, IWriteStream<T>
    {
    }
    class RandomStream<T> : IRandomStream<T>
    {
        private readonly Random rng;

        public RandomStream(Random rng)
        {
            this.rng = rng;
        }
        List<IWriteStream<T>> outputs = new List<IWriteStream<T>>();

        public void pipe(IWriteStream<T> stream)
        {
            outputs.Add(stream);
        }

        public async void SendMessage(string addressTo, string addressFrom, T message)
        {
            int delay;
            lock(rng)
            {
                delay = rng.Next(1);
            }
            await Task.Delay(delay);
            foreach (var output in outputs)
            {
                output.SendMessage(addressTo, addressFrom, message);
            }
        }
    }

    public class item<T>
    {
        public string addressTo;
        public string addressFrom;
        public T message;
        public void Send(IWriteStream<T> reciever)
        {
            reciever.SendMessage(addressTo, addressFrom, message);
        }
    }

    class ManualStream<T> : IWriteStream<T>
    {
        private readonly ITaskQueue<item<T>> queue = new TaskQueue<item<T>>();


        public void SendMessage(string addressTo, string addressFrom, T message)
        {
            queue.Enqueue(new item<T>
            {
                addressTo = addressTo,
                addressFrom = addressFrom,
                message = message
            });
        }
        public Task<item<T>> nextMessage()
        {
            return queue.Dequeue(TimeSpan.FromSeconds(20), CancellationToken.None);
        }
    }

    [TestClass]
    public class InteractionTest
    {

        //drop first message from proposer to acceptor
        [TestMethod]
        public async Task TestCaseA()
        {
            var proposer = new Proposer("prop-A", 1);
            var propStream = proposer.Pipe(new ManualStream<NetworkMessage>());
            var acceptor = new Acceptor();
            var accStream = acceptor.Pipe(new ManualStream<NetworkMessage>());
            var res = proposer.Propose("foo", CancellationToken.None);
            await propStream.nextMessage();
            (await propStream.nextMessage()).Send(acceptor);
            (await accStream.nextMessage()).Send(proposer);
            (await propStream.nextMessage()).Send(acceptor);
            (await accStream.nextMessage()).Send(proposer);
            Assert.AreEqual(await res, "foo");
        }
        [TestMethod]
        public async Task TestRandom()
        {
            var rng = new Random(512);
            var results = new List<KeyValuePair<string, Task>>();

            for (var p = 1; p <= 5; p++)
            {
                for (int a = 1; a <= 5; a++)
                {
                    results.Add(new KeyValuePair<string, Task>("(" + p + "," + a + ")", TestInteraction(p, a, rng)));
                }
            }


            foreach (var result in results)
            {
                Trace.WriteLine("Begin wait for " + result.Key);
                await result.Value;
                Trace.WriteLine("End wait for " + result.Key);
            }
        }

        private async Task TestInteraction(int Proposers, int Acceptors, Random rng)
        {
            List<Acceptor> acceptors = Enumerable.Range(0, Acceptors).Select((i) => new Acceptor()).ToList();
            List<Proposer> proposers = Enumerable.Range(0, Proposers).Select((i) => new Proposer(Guid.NewGuid().ToString(), Acceptors)).ToList();
            foreach (var acceptor in acceptors)
            {
                foreach (var proposer in proposers)
                {
                    acceptor.Pipe(new RandomStream<NetworkMessage>(rng)).Pipe(proposer).Pipe(new RandomStream<NetworkMessage>(rng)).Pipe(acceptor);
                }
            }

            var proposed = new HashSet<string>();

            var results = new Queue<Task<string>>();
            foreach (var proposer in proposers)
            {
                var proposal = Guid.NewGuid().ToString();
                results.Enqueue(proposer.Propose(proposal, CancellationToken.None));
                proposed.Add(proposal);
            }
            var answer = await results.Dequeue();
            Assert.IsTrue(proposed.Contains(answer), "The result must be proposed by one of the proposers");
            while (results.Any())
            {
                Assert.AreEqual(answer, await results.Dequeue());
            }
        }
    }
}
