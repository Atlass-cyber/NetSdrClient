using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography; // Це було в тебе, але для HashCode не потрібно
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking 
{
    public class UdpClientWrapper : IUdpClient, IDisposable
    {
        private readonly IPEndPoint _localEndPoint;
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;
        private bool _disposedValue = false;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {
            _cts = new CancellationTokenSource();
            Console.WriteLine("Start listening for UDP messages...");

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                while (!_cts.Token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);

                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException)
            {
                // ВИПРАВЛЕНО: Додаємо лог, щоб блок не був порожнім
                Console.WriteLine("Listening task was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose(); // <-- ВИПРАВЛЕННЯ (Reliability): Очищуємо CTS
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping: {ex.Message}");
            }
        }

        public void Exit()
        {
            StopListening();
        }

        // Sonar fix: Equals узгоджене з GetHashCode
        public override bool Equals(object? obj)
        {
            if (obj is not UdpClientWrapper other)
                return false;

            return _localEndPoint.Address.Equals(other._localEndPoint.Address)
                   && _localEndPoint.Port == other._localEndPoint.Port;
        }

        public override int GetHashCode()
        {
            // === ВИПРАВЛЕННЯ (MD5 Hotspot) ===
            // Замінюємо повільний MD5 на швидкий і правильний HashCode.Combine
            return HashCode.Combine(nameof(UdpClientWrapper), _localEndPoint.Address, _localEndPoint.Port);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _udpClient?.Close();
                    _udpClient?.Dispose();
                }

                _cts = null;
                _udpClient = null;
                _disposedValue = true;
        }
}

public void Dispose()
{
    Dispose(disposing: true);
    GC.SuppressFinalize(this); // <-- Саме цей рядок вимагав Sonar
}
    }
}
