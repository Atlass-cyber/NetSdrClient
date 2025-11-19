using System;
using System.Net;
using System.Net.Sockets;
using NetSdrClientApp.Networking; 

namespace NetSdrClientApp.Networking
{
    public class UdpSenderWrapper : IUdpSender 
    {
        private readonly UdpClient _udpClient;
        private bool disposedValue; // ДОБАВЛЕНО

        public UdpSenderWrapper()
        {
            _udpClient = new UdpClient();
        }

        public void Send(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            _udpClient.Send(datagram, bytes, endPoint);
        }

        // ИЗМЕНЕНО: Полный паттерн Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _udpClient.Dispose(); // Очистка управляемого ресурса
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