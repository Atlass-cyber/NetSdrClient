using System.Net;
using System.Net.Sockets;
using NetSdrClientApp.Networking; 

namespace NetSdrClientApp.Networking
{
    public class UdpSenderWrapper : IUdpSender 
    {
        private readonly UdpClient _udpClient;

        public UdpSenderWrapper()
        {
            _udpClient = new UdpClient();
        }

        public void Send(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            _udpClient.Send(datagram, bytes, endPoint);
        }

        public void Dispose()
        {
            _udpClient.Dispose();
        }
    }
}