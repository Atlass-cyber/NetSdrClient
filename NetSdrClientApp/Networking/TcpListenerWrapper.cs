using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;

namespace NetSdrClientApp.Networking
{
    public class TcpListenerWrapper : ITcpListener
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(IPAddress localaddr, int port)
        {
            _listener = new TcpListener(localaddr, port);
        }

        public void Start() => _listener.Start();
        
        public void Stop() => _listener.Stop();

        public async Task<ITcpConnection> AcceptTcpClientAsync()
        {
            // Оборачиваем реальный TcpClient в наш ITcpConnection
            TcpClient client = await _listener.AcceptTcpClientAsync();
            return new TcpConnectionWrapper(client);
        }
    }
}