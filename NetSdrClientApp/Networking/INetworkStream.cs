using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace NetSdrClientApp.Networking
{
    // Абстракция NetworkStream
    public interface INetworkStream : IDisposable
    {
        Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken token);
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token);
    }
}