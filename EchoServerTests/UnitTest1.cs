using NUnit.Framework;
using NetSdrClientApp.Server; 

namespace EchoServerTests
{
    public class EchoLogicTests
    {
        [Test] 
        public void ProcessMessage_ShouldReturnSameBytes_WhenCalled()
        {
            byte[] inputMessage = new byte[] { 10, 20, 30 };
            byte[] expectedResponse = new byte[] { 10, 20, 30 };
            byte[] actualResponse = EchoLogic.ProcessMessage(inputMessage, inputMessage.Length);

            Assert.That(actualResponse, Is.EqualTo(expectedResponse));
        }

        [Test]
        public void ProcessMessage_ShouldHandlePartialBuffer_WhenCalledCorrectly()
        {
            // Arrange
            byte[] largeInputBuffer = new byte[] { 1, 2, 3, 0, 0, 0, 0 }; 
            int bytesToProcess = 3; 
            byte[] expectedResponse = new byte[] { 1, 2, 3 };

            // Act
            byte[] actualResponse = EchoLogic.ProcessMessage(largeInputBuffer, bytesToProcess);

            // Assert
            Assert.That(actualResponse, Is.EqualTo(expectedResponse), "Логіка мала 'обрізати' буфер до реального розміру");
        }
    }
}
