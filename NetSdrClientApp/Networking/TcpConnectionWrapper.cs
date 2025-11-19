using System.Net.Sockets;
using NetSdrClientApp.Networking;

namespace NetSdrClientApp.Networking
{
    // Обертка для TCP-соединения, принятого сервером
    public class TcpConnectionWrapper : ITcpConnection 
    {
        private readonly TcpClient _client;

        public TcpConnectionWrapper(TcpClient client)
        {
            _client = client;
        }

        public INetworkStream GetStream()
        {
            // Возвращаем нашу обертку потока
            return new NetworkStreamWrapper(_client.GetStream());
        }

        public void Close() => _client.Close();
        
        public void Dispose() => _client.Dispose();
    }
}