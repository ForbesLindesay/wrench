using Paxos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph_Generator
{
    class Drop<T> : IDuplexStream<T>
    {
        private static readonly Random defaultRandom = new Random();

        private readonly Random random;
        private readonly double percentage;
        private readonly ConcurrentBag<IWriteStream<T>> listeners = new ConcurrentBag<IWriteStream<T>>();

        private bool dropNext()
        {
            return random.NextDouble() <= percentage;
        }

        public Drop(double Percentage, Random Random = null)
        {
            if (percentage > 1 || percentage < 0) throw new ArgumentException("Percentage must be between 0 and 1", "Percentage");
            if (Random == null) Random = defaultRandom;
            random = Random;
            percentage = Percentage;
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
        private void SendMessage(IWriteStream<T> listener, string addressTo, string addressFrom, T message)
        {
            if (!dropNext()) listener.SendMessage(addressTo, addressFrom, message);
        }
    }
}
