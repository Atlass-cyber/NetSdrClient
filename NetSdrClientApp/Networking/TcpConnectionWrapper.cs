using System;
using System.Net.Sockets;
using NetSdrClientApp.Networking;

namespace NetSdrClientApp.Networking
{
    // Обертка для TCP-соединения, принятого сервером
    public class TcpConnectionWrapper : ITcpConnection 
    {
        private readonly TcpClient _client;
        private bool disposedValue; // ДОБАВЛЕНО

        public TcpConnectionWrapper(TcpClient client)
        {
            _client = client;
        }

        public INetworkStream GetStream()
        {
            // NOTE: Предполагается, что NetworkStreamWrapper находится в том же namespace
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close() => _client.Close();
        
        // ИЗМЕНЕНО: Полный паттерн Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Закрытие клиента также очищает поток
                    _client.Dispose();
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