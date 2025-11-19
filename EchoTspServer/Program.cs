using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

// Новые usings для внедрения зависимостей
using NetSdrClientApp.Networking; 
using NetSdrClientApp.Utils; 
using AppITimer = NetSdrClientApp.Utils.ITimer; 

namespace NetSdrClientApp.Server 
{
    public class EchoServer
    {
        private readonly int _port;
        private ITcpListener? _listener; 
        private readonly CancellationTokenSource _cancellationTokenSource; 

        private readonly Func<IPAddress, int, ITcpListener> _listenerFactory;
        
        public EchoServer(int port, Func<IPAddress, int, ITcpListener> listenerFactory)
        {
            _port = port;
            _listenerFactory = listenerFactory;
            _cancellationTokenSource = new CancellationTokenSource();
        }
    
        public async Task StartAsync()
        {
            _listener = _listenerFactory(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");
    
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // ДОБАВЛЕНО: Проверка перед Accept
                    if (_cancellationTokenSource.IsCancellationRequested) break; 
                    
                    ITcpConnection client = await _listener.AcceptTcpClientAsync(); 
                    Console.WriteLine("Client connected.");
    
                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
    
            Console.WriteLine("Server shutdown.");
        }
    
        public static async Task HandleClientAsync(ITcpConnection client, CancellationToken token) 
        {
            using (INetworkStream stream = client.GetStream()) 
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
    
                    while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        byte[] response = EchoLogic.ProcessMessage(buffer, bytesRead);
                        await stream.WriteAsync(response, 0, response.Length, token); 
                        Console.WriteLine($"Echoed {response.Length} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close(); 
                    Console.WriteLine("Client disconnected.");
                }
            }
        }
    
        // ИСПРАВЛЕНО: УБРАН CancellationTokenSource.Dispose()
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop(); 
            Console.WriteLine("Server stopped.");
        }
    
        public static void Main(string[] args) 
        {
            Func<IPAddress, int, ITcpListener> listenerFactory = 
                (addr, port) => new NetSdrClientApp.Networking.TcpListenerWrapper(addr, port);
            
            EchoServer server = new EchoServer(5000, listenerFactory);
    
            _ = Task.Run(() => server.StartAsync());
    
            string host = "127.0.0.1"; 
            int port = 60000;         
            int intervalMilliseconds = 5000; 
    
            // --- UdpTimedSender ---
            using (var udpSender = new UdpSenderWrapper())
            using (AppITimer systemTimer = new SystemTimerWrapper())
            {
                using (var sender = new UdpTimedSender(host, port, udpSender, systemTimer))
                {
                    Console.WriteLine("Press any key to stop sending...");
                    sender.StartSending(intervalMilliseconds);
    
                    Console.WriteLine("Press 'q' to quit...");
                    while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                    {
                    }
    
                    sender.StopSending();
                    server.Stop();
                    Console.WriteLine("Sender stopped.");
                }
            }
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        
        private readonly IUdpSender _udpClient;
        private readonly AppITimer _timer; 
    
        public UdpTimedSender(string host, int port, IUdpSender udpClient, AppITimer timer)
        {
            _host = host;
            _port = port;
            _udpClient = udpClient;
            _timer = timer;
        }
    
        public void StartSending(int intervalMilliseconds)
        {
            _timer.Start(intervalMilliseconds, SendMessageCallback, null);
        }
    
        ushort i = 0;
    
        public byte[] CreateMessage() 
        {
            byte[] samples = new byte[1024];
        
            using (var rnd = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rnd.GetBytes(samples);
            }

            i++;
            return (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(i)).Concat(samples).ToArray();
        }
        
        private void SendMessageCallback(object? state) 
        {
            try
            {
                byte[] msg = CreateMessage();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
    
                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port} ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
    
        public void StopSending()
        {
            _timer.Dispose();
        }
    
        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
            GC.SuppressFinalize(this); 
        }
    }
}