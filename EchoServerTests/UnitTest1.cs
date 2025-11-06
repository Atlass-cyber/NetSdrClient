using NUnit.Framework;
using NetSdrClientApp.Server; 
// using NUnit.Framework.Legacy; - Більше не потрібен

namespace EchoServerTests
{
    public class EchoLogicTests
    {
        [Test] 
        public void ProcessMessage_ShouldReturnSameBytes_WhenCalled()
        {
            var logic = new EchoLogic();      
            byte[] inputMessage = new byte[] { 10, 20, 30 };
            byte[] expectedResponse = new byte[] { 10, 20, 30 };
            byte[] actualResponse = logic.ProcessMessage(inputMessage, inputMessage.Length);

            // Використовуємо NUnit 4 синтаксис "Constraint Model"
            Assert.That(actualResponse, Is.EqualTo(expectedResponse));
        }

        [Test]
        public void ProcessMessage_ShouldHandlePartialBuffer_WhenCalledCorrectly()
        {
            // Arrange
            var logic = new EchoLogic();
            byte[] largeInputBuffer = new byte[] { 1, 2, 3, 0, 0, 0, 0 }; 
            int bytesToProcess = 3; 
            byte[] expectedResponse = new byte[] { 1, 2, 3 };

            // Act
            byte[] actualResponse = logic.ProcessMessage(largeInputBuffer, bytesToProcess);

            // Assert
            // Використовуємо NUnit 4 синтаксис "Constraint Model"
            Assert.That(actualResponse, Is.EqualTo(expectedResponse), "Логіка мала 'обрізати' буфер до реального розміру");
        }
    }
}
