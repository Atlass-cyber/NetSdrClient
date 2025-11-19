using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class NetworkStreamWrapper : INetworkStream
    {
        private readonly NetworkStream _stream;

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken token) => 
            _stream.ReadAsync(buffer, offset, size, token);
        
        public Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token) =>
            _stream.WriteAsync(buffer, offset, size, token);
            
        public void Dispose() => _stream.Dispose();
    }
}