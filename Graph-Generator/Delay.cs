using Paxos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph_Generator
{
    class Delay<T> : IDuplexStream<T>
    {
        private static readonly Random defaultRandom = new Random();

        private readonly Random random;
        private readonly TimeSpan minimum;
        private readonly TimeSpan maximum;
        private readonly ConcurrentBag<IWriteStream<T>> listeners = new ConcurrentBag<IWriteStream<T>>();

        private TimeSpan next()
        {
            int range = (int)(maximum.Ticks - minimum.Ticks);
            return new TimeSpan(random.Next(range) + minimum.Ticks);
        }

        public Delay(TimeSpan Amount)
        {
            random = defaultRandom;
            minimum = Amount;
            maximum = Amount;
        }
        public Delay(TimeSpan Minimum, TimeSpan Maximum, Random Random = null)
        {
            if (Random == null) Random = defaultRandom;
            random = Random;
            minimum = Minimum;
            maximum = Maximum;
        }



        public void pipe(IWriteStream<T> stream)
        {
            listeners.Add(stream);
        }

        public void SendMessage(string addressTo, string addressFrom, T message)
        {
            foreach (var listener in listeners)
            {
                SendMessage(listener, addressTo, addressFrom, message);
            }
        }
        private async void SendMessage(IWriteStream<T> listener, string addressTo, string addressFrom, T message)
        {
            await Task.Delay(next());
            listener.SendMessage(addressTo, addressFrom, message);
        }
    }
}
