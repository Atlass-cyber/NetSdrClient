using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework; // <-- Вот замена
using NetSdrClientApp.Networking; 

namespace NetSdrClientApp.Tests
{
    public class TcpClientWrapperTests
    {
        [Test] // <-- Вот замена
        public async Task SendMessageAsync_String_ShouldSendCorrectBytes()
        {
            // ARRANGE (Подготовка)
            // 1. Запускаем наш фейковый "мини-сервер"
            var listener = new TcpListener(IPAddress.Loopback, 0); // 0 = взять любой свободный порт
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            string messageReceivedByServer = ""; // Сюда запишем то, что получит сервер
            Exception serverError = null;

            // 2. Запускаем сервер в фоновом потоке, он ждет 1 подключение
            var listenerTask = Task.Run(async () =>
            {
                try
                {
                    // Ждем подключения
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();
                    
                    // Читаем данные, которые пришлет наш wrapper
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    // Конвертируем в строку
                    messageReceivedByServer = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
                catch(Exception ex)
                {
                    serverError = ex;
                }
                finally
                {
                    listener.Stop(); // Выключаем сервер
                }
            });

            // 3. Создаем наш клиент, который будет подключаться к мини-серверу
            var wrapper = new TcpClientWrapper("127.0.0.1", port);

            // ACT (Действие)
            string testMessage = "hello_sonar";
            
            // 4. Подключаемся
            wrapper.Connect(); 

            // 5. Вызываем тот самый метод, который мы рефакторили!
            await wrapper.SendMessageAsync(testMessage);

            // 6. Ждем, пока фоновый сервер получит сообщение и завершит работу
            await listenerTask;
            wrapper.Disconnect();

            // ASSERT (Проверка)
            
            // 7. Проверяем, что сервер не упал с ошибкой
            Assert.Null(serverError);
            
            // 8. Проверяем, что сервер получил *в точности* то, что мы отправили
            Assert.AreEqual(testMessage, messageReceivedByServer); // Assert.Equal тоже сработал бы, но AreEqual более "классический" для NUnit
            
            // 9. (Бонус) Проверяем, что клиент отключился
            Assert.False(wrapper.Connected);
        }
    }
}
