using System;
using System.Net.Sockets;

namespace NetSdrClientApp.Networking
{
    // Представляет TCP-соединение, принятое сервером.
    public interface ITcpConnection : IDisposable
    {
        INetworkStream GetStream();
        void Close();
    }
}