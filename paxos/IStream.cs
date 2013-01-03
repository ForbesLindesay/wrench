using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paxos
{
    public interface IDuplexStream<T> : IReadStream<T>, IWriteStream<T>
    {}
    public interface IReadStream<T>
    {
        void pipe(IWriteStream<T> stream);
    }
    public interface IWriteStream<T>
    {
        void SendMessage(string addressTo, string addressFrom, T message);
    }

    public static class ReadStreamExtensions
    {
        public static S Pipe<T, S>(this IReadStream<T> inStream, S outStream) where S : IWriteStream<T>
        {
            inStream.pipe(outStream);
            return outStream;
        }
    }
}
