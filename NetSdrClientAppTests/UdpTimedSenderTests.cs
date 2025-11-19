using NUnit.Framework;
using Moq;
using System;
using System.Net;
using System.Threading;
using NetSdrClientApp.Networking; 
using NetSdrClientApp.Utils;     
using NetSdrClientApp.Server;    

// Используем псевдоним, чтобы избежать конфликта с System.Threading.ITimer
using AppITimer = NetSdrClientApp.Utils.ITimer; 
using static NUnit.Framework.Is; 

[TestFixture] 
public class UdpTimedSenderTests
{
    private Mock<IUdpSender> _mockUdpSender;
    private Mock<AppITimer> _mockTimer;

    [SetUp] 
    public void Setup()
    {
        // Инициализируем моки перед каждым тестом
        _mockUdpSender = new Mock<IUdpSender>();
        _mockTimer = new Mock<AppITimer>();
    }

    private UdpTimedSender CreateSender()
    {
        // Создаем тестируемый объект, передавая ему моки
        return new UdpTimedSender("127.0.0.1", 60000, _mockUdpSender.Object, _mockTimer.Object);
    }

    [Test] 
    public void Constructor_InitializesFields()
    {
        var sender = CreateSender();
        Assert.That(sender, Is.Not.Null); 
    }

    [Test]
    public void CreateMessage_GeneratesCorrectPacketStructure()
    {
        var sender = CreateSender();

        var message1 = sender.CreateMessage();
        Assert.That(message1.Length, Is.EqualTo(2 + sizeof(ushort) + 1024));
        Assert.That(message1[0], Is.EqualTo(0x04));
        Assert.That(message1[1], Is.EqualTo(0x84));

        var message2 = sender.CreateMessage();
        var i1 = BitConverter.ToUInt16(message1, 2);
        var i2 = BitConverter.ToUInt16(message2, 2);
        Assert.That(i2, Is.EqualTo(i1 + 1));
    }

    [Test]
    public void StartSending_CallsTimerStartWithCorrectInterval()
    {
        var sender = CreateSender();
        const int interval = 5000;
        
        sender.StartSending(interval);

        // Проверяем, что метод Start был вызван на моке таймера с нужным колбэком и интервалом
        _mockTimer.Verify(t => t.Start(
            interval, 
            It.IsAny<TimerCallback>(), 
            It.IsAny<object?>()), 
            Times.Once);
    }

    [Test]
    public void StopSending_And_Dispose_CallsDisposeOnTimerAndUdpSender()
    {
        var sender = CreateSender();

        // 1. Проверяем StopSending (должен диспоузить только таймер)
        sender.StopSending();
        _mockTimer.Verify(t => t.Dispose(), Times.Once); 
        _mockUdpSender.Verify(s => s.Dispose(), Times.Never); 
        
        // 2. Проверяем Dispose (должен диспоузить отправитель)
        sender.Dispose();
        _mockUdpSender.Verify(s => s.Dispose(), Times.Once); 
    }
    
    [Test]
    public void SendMessageCallback_ExecutesSendingLogic()
    {
        // ARRANGE: Настраиваем мок таймера, чтобы "захватить" TimerCallback (колбэк)
        TimerCallback capturedCallback = null;
        _mockTimer
            .Setup(t => t.Start(It.IsAny<int>(), It.IsAny<TimerCallback>(), It.IsAny<object?>()))
            .Callback<int, TimerCallback, object?>((interval, callback, state) => capturedCallback = callback);

        var sender = CreateSender();
        sender.StartSending(100);

        // ACT: Вызываем захваченный callback (имитируем срабатывание таймера)
        Assert.That(capturedCallback, Is.Not.Null); 
        capturedCallback.Invoke(null); 

        // ASSERT: Проверяем, что метод Send был вызван на IUdpSender
        _mockUdpSender.Verify(c => c.Send(
            It.IsAny<byte[]>(), 
            1028, // Проверяем, что длина пакета верна
            It.IsAny<IPEndPoint>()), 
            Times.Once);
    }
}