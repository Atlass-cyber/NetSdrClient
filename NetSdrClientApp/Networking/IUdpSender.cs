using System;
using System.Net;

namespace NetSdrClientApp.Networking
{ 
    public interface IUdpSender : IDisposable
    {
        void Send(byte[] datagram, int bytes, IPEndPoint endPoint);
    }
}