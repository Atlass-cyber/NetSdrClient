using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class NetworkStreamWrapper : INetworkStream
    {
        private readonly NetworkStream _stream;
        private bool disposedValue; // ДОБАВЛЕНО

        public NetworkStreamWrapper(NetworkStream stream)
        {
            _stream = stream;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken token) => 
            _stream.ReadAsync(buffer, offset, size, token);
        
        public Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken token) =>
            _stream.WriteAsync(buffer, offset, size, token);
            
        // ИЗМЕНЕНО: Полный паттерн Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stream.Dispose(); // Очистка управляемого ресурса
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}