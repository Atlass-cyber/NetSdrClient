using NUnit.Framework;
using Moq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetSdrClientApp.Networking; 
using NetSdrClientApp.Server;
using static NUnit.Framework.Is; 

namespace EchoServerTestsCoverage 
{
    [TestFixture]
    public class EchoServerTests
    {
        private Mock<ITcpListener> _mockListener;
        private Mock<ITcpConnection> _mockConnection;
        private Mock<INetworkStream> _mockStream;
        private Func<IPAddress, int, ITcpListener> _listenerFactory;

        [SetUp]
        public void Setup()
        {
            // 1. Мокирование потока (имитация клиента)
            _mockStream = new Mock<INetworkStream>();
            
            // 2. Мокирование соединения (принимается слушателем)
            _mockConnection = new Mock<ITcpConnection>();
            _mockConnection.Setup(c => c.GetStream()).Returns(_mockStream.Object);

            // 3. Мокирование слушателя
            _mockListener = new Mock<ITcpListener>();
            _listenerFactory = (addr, port) => _mockListener.Object;
        }

        private EchoServer CreateServer()
        {
            // Создаем тестируемый объект, используя нашу фабрику моков
            return new EchoServer(5000, _listenerFactory);
        }

        // =========================================================
        // ТЕСТЫ ЛОГИКИ ЗАПУСКА И ОСТАНОВКИ (StartAsync, Stop)
        // =========================================================

        [Test]
        public async Task StartAsync_CallsStartOnListenerAndHandlesDisposedException()
        {
            var server = CreateServer();
            
            // ARRANGE: Настраиваем, чтобы AcceptTcpClientAsync вызвал ObjectDisposedException
            // Это имитирует ситуацию, когда Stop() вызывается во время ожидания Accept.
            _mockListener
                .Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("Listener shutdown"));

            // ACT
            await server.StartAsync();

            // ASSERT
            // 1. Проверяем, что Start был вызван
            _mockListener.Verify(l => l.Start(), Times.Once); 
            // 2. Проверяем, что Accept был вызван (хотя бы один раз, прежде чем упал)
            _mockListener.Verify(l => l.AcceptTcpClientAsync(), Times.AtLeastOnce);
        }

        [Test]
        public async Task Stop_CallsCancelAndStopOnListener()
        {
            var server = CreateServer();
            
            // ARRANGE: Настраиваем AcceptTcpClientAsync, чтобы он ждал долго
            _mockListener
                .Setup(l => l.AcceptTcpClientAsync())
                .Returns(Task.Delay(5000).ContinueWith(_ => _mockConnection.Object)); 
            
            _mockListener.Setup(l => l.Start());

            // ACT: 
            // Запускаем StartAsync в фоновом режиме (Task)
            var startTask = server.StartAsync(); 
            
            // Ждем небольшую паузу, чтобы цикл гарантированно запустился
            await Task.Delay(50); 

            server.Stop(); // Вызываем Stop

            // Ждем завершения startTask. Он должен завершиться без исключения.
            await startTask; 

            // ASSERT
            _mockListener.Verify(l => l.Stop(), Times.Once); 
            _mockListener.Verify(l => l.AcceptTcpClientAsync(), Times.AtLeastOnce);
        }

        // =========================================================
        // ТЕСТЫ ЛОГИКИ ОБРАБОТКИ КЛИЕНТА (HandleClientAsync)
        // =========================================================

        [Test]
        public async Task HandleClientAsync_ShouldEchoMessageAndCloseConnection()
        {
            // ARRANGE
            byte[] inputData = { 0x01, 0x02, 0x03 };
            
            // Настройка чтения: 
            // 1. Первое чтение возвращает данные.
            // 2. Второе чтение возвращает 0 (имитация закрытия соединения клиентом).
            var sequence = new MockSequence();
            _mockStream.InSequence(sequence)
                .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(inputData.Length)
                .Callback<byte[], int, int, CancellationToken>((buffer, offset, size, token) => Array.Copy(inputData, 0, buffer, 0, inputData.Length)); 
            _mockStream.InSequence(sequence)
                .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // ACT
            var token = CancellationToken.None;
            await EchoServer.HandleClientAsync(_mockConnection.Object, token);

            // ASSERT
            // 1. Проверяем, что была запись в поток (эхо)
            _mockStream.Verify(s => s.WriteAsync(
                It.IsAny<byte[]>(), 
                0, // ИСПРАВЛЕНО: offset = 0
                inputData.Length, 
                token), 
                Times.Once);

            // 2. Проверяем, что соединение было закрыто в блоке finally
            _mockConnection.Verify(c => c.Close(), Times.Once);
            _mockStream.Verify(s => s.Dispose(), Times.Once); 
        }
        
        // ТЕСТ НА ОБРАБОТКУ ИСКЛЮЧЕНИЙ
        [Test]
        public async Task HandleClientAsync_ShouldHandleGenericException()
        {
            // ARRANGE: Настраиваем поток, чтобы выбросить ошибку при чтении
            _mockStream
                .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Simulated network error"));

            // ACT & ASSERT: Ожидаем, что метод не выбросит исключение и закроет соединение
            var token = CancellationToken.None;
            Assert.That(async () => await EchoServer.HandleClientAsync(_mockConnection.Object, token), Throws.Nothing);

            // Проверяем, что Close был вызван (блок finally)
            _mockConnection.Verify(c => c.Close(), Times.Once);
        }
    }
}