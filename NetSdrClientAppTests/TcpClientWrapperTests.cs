using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
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
                    // ВИПРАВЛЕНО: Прибрано зайвий символ "_"
                    listener.Stop();
                    }
            });

            
            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            
            string testMessage = "hello_sonar";
                             
            wrapper.Connect(); 
            
            await wrapper.SendMessageAsync(testMessage);

            await listenerTask;
            wrapper.Disconnect();
                  
            // Виправлено: NUnit 4 синтаксис
            Assert.That(serverError, Is.Null);
            
            
            // Виправлено: NUnit 4 синтаксис (actual, expected)
            Assert.That(messageReceivedByServer, Is.EqualTo(testMessage)); 
                            
            // Виправлено: NUnit 4 синтаксис
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void SendMessageAsync_String_WhenNotConnected_ShouldThrowException()
        {
            
            var wrapper = new TcpClientWrapper("127.0.0.1", 1234);
            
            // Цей синтаксис (Assert.ThrowsAsync) коректний для NUnit 3 та 4
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
              {
                await wrapper.SendMessageAsync("test message");
            });

            // Виправлено: NUnit 4 синтаксис
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Connect_Should_Handle_Connection_Failure()
        {
            // Використовуємо порт, який ніхто не слухає
            var wrapper = new TcpClientWrapper("127.0.0.1", 65534);

            // Act
            wrapper.Connect();

            // Assert
            // Цей тест виконує код у catch { _tcpClient.Close(); _tcpClient = null; }
            Assert.That(wrapper.Connected, Is.False);
        }

        // === НОВИЙ ТЕСТ 2: Покриває Disconnect() [7 з 9 рядків] ===
        [Test]
        public async Task Disconnect_Should_Close_Connection_And_Dispose_CTS()
        {
            // Arrange: Створюємо реальний слухач
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            
            var listenerTask = Task.Run(async () =>
            {
                // Просто приймаємо з'єднання, щоб Connect() пройшов
                try { await listener.AcceptTcpClientAsync(); } catch { /* Ігноруємо помилки */ }
            });

            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            
            // Act: Підключаємося і ВІДРАЗУ відключаємося
            wrapper.Connect();
            var wasConnected = wrapper.Connected;
            wrapper.Disconnect();
            
            await listenerTask;
            listener.Stop();

            // Assert
            Assert.That(wasConnected, Is.True, "Мав підключитися");
            Assert.That(wrapper.Connected, Is.False, "Мав відключитися");
            // Цей тест виконує 7 рядків у if(Connected) в методі Disconnect()
        }
          
    }
}

