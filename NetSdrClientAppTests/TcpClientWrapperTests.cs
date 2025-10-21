using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework; // <-- Вот замена
using NetSdrClientApp.Networking; 

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
            Exception serverError = null;

            
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
                catch(Exception ex)
                {
                    serverError = ex;
                }
                finally
                {
                    listener.Stop(); /
                }
            });

            
            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            
            string testMessage = "hello_sonar";
                        
            wrapper.Connect(); 
            
            await wrapper.SendMessageAsync(testMessage);

            await listenerTask;
            wrapper.Disconnect();
                  
            Assert.Null(serverError);
            
            
            Assert.AreEqual(testMessage, messageReceivedByServer); 
                        
            Assert.False(wrapper.Connected);
        }

        [Test]
        public void SendMessageAsync_String_WhenNotConnected_ShouldThrowException()
        {
            
            var wrapper = new TcpClientWrapper("127.0.0.1", 1234);
           
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
            {
                await wrapper.SendMessageAsync("test message");
            });

            Assert.False(wrapper.Connected);
        }
    }
}
