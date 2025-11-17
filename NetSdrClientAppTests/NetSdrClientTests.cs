using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

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
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
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

    // --- ТЕСТ №1 (адаптований під NUnit): Перевіряємо, що Disconnect викликається навіть якщо не було підключення ---
    [Test]
    public Task Disconnect_ShouldAlwaysCallDisconnectOnTcpClient()
    {
        // Arrange - нічого не потрібно, все є в Setup

        // Act
        _client.Disconect();

        // Assert
        // Просто перевіряємо, що метод Disconnect нашого фальшивого клієнта був викликаний 1 раз
        _tcpMock.Verify(c => c.Disconnect(), Times.Once());
    }

    // --- ТЕСТ №2 (адаптований під NUnit): Перевіряємо, що StopIQAsync не працює, якщо немає підключення ---
    [Test]
    public async Task StopIQAsync_ShouldDoNothing_WhenNotConnected()
    {
        // Arrange
        // Кажемо, що клієнт НЕ підключений
        _tcpMock.Setup(c => c.Connected).Returns(false);
        _client.IQStarted = true; // Імітуємо, що IQ було запущено

        // Act
        await _client.StopIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.True, "Статус IQ не повинен був змінитись, бо немає підключення.");
        _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
    }

    // --- ТЕСТ №3 (адаптований під NUnit): Перевіряємо, що ChangeFrequencyAsync відправляє повідомлення ---
    [Test]
    public async Task ChangeFrequencyAsync_ShouldSendMessage_WhenCalled()
    {
        // Arrange
        // Кажемо, що клієнт підключений, щоб метод відпрацював
        _tcpMock.Setup(c => c.Connected).Returns(true);

        // Act
        await _client.ChangeFrequencyAsync(1000000, 1);

        // Assert
        _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Once(), "Метод для відправки повідомлення не був викликаний.");
    }

    // --- ТЕСТ №4 (адаптований під NUnit): Перевіряємо, що ConnectAsync нічого не робить, якщо вже є підключення ---
    [Test]
    public async Task ConnectAsync_ShouldDoNothing_WhenAlreadyConnected()
    {
        // Arrange
        // Кажемо, що клієнт ВЖЕ підключений
        _tcpMock.Setup(c => c.Connected).Returns(true);
        // Скидаємо лічильники викликів, які могли спрацювати в Setup
        _tcpMock.Invocations.Clear(); 

        // Act
        await _client.ConnectAsync();

        // Assert
        // Перевіряємо, що метод Connect НЕ був викликаний знову
        _tcpMock.Verify(c => c.Connect(), Times.Never(), "Метод Connect не повинен викликатись, якщо вже є підключення.");
        _tcpMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
    }
}
