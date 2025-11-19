using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public interface ITcpListener
    {
        void Start();
        void Stop();
        Task<ITcpConnection> AcceptTcpClientAsync(); // Используем новый интерфейс ITcpConnection
    }
}