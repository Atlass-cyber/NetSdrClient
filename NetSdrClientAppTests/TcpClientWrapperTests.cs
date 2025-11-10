using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using NetSdrClientApp.Networking;
using System.Threading.Tasks;

namespace NetSdrClientApp.Tests
{
    public class TcpClientWrapperTests
    {
        [Test]
        public async Task SendMessageAsync_String_ShouldSendCorrectBytes()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            string messageReceivedByServer = "";
            Exception? serverError = null;

            var listenerTask = Task.Run(async () =>
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    messageReceivedByServer = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
                catch (Exception ex)
                {
                    serverError = ex;
                }
                finally
                {
                    listener.Stop();
                }
            });

            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            string testMessage = "hello_sonar";
            
            wrapper.Connect();
            await wrapper.SendMessageAsync(testMessage);

            await listenerTask;
            wrapper.Disconnect();

            // ВИПРАВЛЕНО: Загорнуто в Assert.Multiple
            Assert.Multiple(() =>
            {
                Assert.That(serverError, Is.Null);
                Assert.That(messageReceivedByServer, Is.EqualTo(testMessage));
                Assert.That(wrapper.Connected, Is.False);
            });
        }

        [Test]
        public void SendMessageAsync_String_WhenNotConnected_ShouldThrowException()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 1234);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await wrapper.SendMessageAsync("test message");
            });

            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Connect_Should_Handle_Connection_Failure()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 65534);
            wrapper.Connect();
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public async Task Disconnect_Should_Close_Connection_And_Dispose_CTS()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            
            var listenerTask = Task.Run(async () =>
            {
                try { await listener.AcceptTcpClientAsync(); } catch { }
            });

            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            wrapper.Connect();
            var wasConnected = wrapper.Connected;
            wrapper.Disconnect();
            
            await listenerTask;
            listener.Stop();

            // ВИПРАВЛЕНО: Загорнуто в Assert.Multiple
            Assert.Multiple(() =>
            {
                Assert.That(wasConnected, Is.True, "Мав підключитися");
                Assert.That(wrapper.Connected, Is.False, "Мав відключитися");
            });
        }

        [Test]
        public void Disconnect_When_Not_Connected_Does_Nothing()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 1234);
            Assert.DoesNotThrow(() => wrapper.Disconnect());
            Assert.That(wrapper.Connected, Is.False);
        }
    }
}
