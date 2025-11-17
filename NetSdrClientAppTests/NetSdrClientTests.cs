using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    public class NetSdrClientTests
    {
        NetSdrClient _client;
        Mock<ITcpClient> _tcpMock;
        Mock<IUdpClient> _updMock;

        public NetSdrClientTests() { }

        [SetUp]
        public Task Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            });

            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
            {
                _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
            });

            _updMock = new Mock<IUdpClient>();

            _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);

            return Task.CompletedTask;
        }

        [Test]
        public async Task ConnectAsyncTest()
        {
            //act
            await _client.ConnectAsync();

            //assert
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public Task TaskDisconnectWithNoConnectionTest()
        {
            //act
            _client.Disconect();

            //assert
            //No exception thrown
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
            return Task.CompletedTask;
        }

        [Test]
        public async Task DisconnectTest()
        {
            //Arrange 
            await ConnectAsyncTest();

            //act
            _client.Disconect();

            //assert
            //No exception thrown
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task StartIQNoConnectionTest()
        {

            //act
            await _client.StartIQAsync();

            //assert
            //No exception thrown
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        }

        [Test]
        public async Task StartIQTest()
        {
            //Arrange 
            await ConnectAsyncTest();

            //act
            await _client.StartIQAsync();

            //assert
            //No exception thrown
            _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
        }

        [Test]
        public async Task StopIQTest()
        {
            //Arrange 
            await ConnectAsyncTest();

            //act
            await _client.StopIQAsync();

            //assert
            //No exception thrown
            _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        //TODO: cover the rest of the NetSdrClient code here

        [Test]
        public Task Disconnect_ShouldAlwaysCallDisconnectOnTcpClient()
        {
            // Arrange - нічого не потрібно, все є в Setup

            // Act
            _client.Disconect();

            // Assert
            _tcpMock.Verify(c => c.Disconnect(), Times.Once());
            return Task.CompletedTask;
        }

        [Test]
        public async Task StopIQAsync_ShouldDoNothing_WhenNotConnected()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(false);
            _client.IQStarted = true; 

            // Act
            await _client.StopIQAsync();

            // Assert
            Assert.That(_client.IQStarted, Is.True);
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendMessage_WhenCalled()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(true);

            // Act
            await _client.ChangeFrequencyAsync(1000000, 1);

            // Assert
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once());
        }

        [Test]
        public Task ConnectAsync_ShouldDoNothing_WhenAlreadyConnected()
        {
            // Arrange
            _tcpMock.Setup(c => c.Connected).Returns(true);
            _tcpMock.Invocations.Clear(); 

            // Act
            _client.ConnectAsync();

            // Assert
            _tcpMock.Verify(c => c.Connect(), Times.Never());
            _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
            return Task.CompletedTask;
        }
    }
}
