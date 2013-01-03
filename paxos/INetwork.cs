using NAct;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Paxos
{
    public interface INetwork
    {
        IBroadcastResponse Broadcast(NetworkMessage message);
        void Send(NetworkMessage message);
        Task<NetworkMessage> NextMessage(CancellationToken cancellationToken);

        string Address { get; }
    }

    public interface IBroadcastResponse
    {
        Task<NetworkMessage> NextMessage(CancellationToken cancellationToken);
    }
}
