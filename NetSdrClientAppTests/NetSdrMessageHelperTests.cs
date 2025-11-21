using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection; // Додано
using NetSdrClientApp.Messages;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;
using static NUnit.Framework.Is;

namespace NetSdrClientAppTests.Messages
{
    [TestFixture]
    public class NetSdrMessageHelperTests
    {
        // ... (попередні методи Setup, CreateMessage, GetDataItemMessage, GetControlItemMessage) ...

        // =========================================================
        // ТЕСТИРОВАНИЕ ГРАНИЧНЫХ СЛУЧАЕВ ЗАГОЛОВКА (GetHeader)
        // ... (методы GetHeader_ThrowsException) ...
        
        [Test]
        public void GetHeader_HandlesDataItemEdgeCase()
        {
            // ARRANGE: bodyLength = 8192. Type = DataItem0 (4). Header value should be 4 << 13 = 32768 (0x8000)
            int bodyLength = 8192;
            
            // ACT
             var result = (byte[])typeof(NetSdrMessageHelper)
                .GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { MsgTypes.DataItem0, bodyLength });

            // ASSERT
            var headerValue = BitConverter.ToUInt16(result);
            
            Assert.That((headerValue >> 13), Is.EqualTo((int)MsgTypes.DataItem0)); 
            Assert.That((headerValue & 0x1FFF), Is.EqualTo(0)); 
        }

        // =========================================================
        // ТЕСТИРОВАНИЕ ПАРСИНГА СООБЩЕНИЯ (TranslateMessage)
        // =========================================================
        
        [Test]
        public void TranslateMessage_ControlItem_Success()
        {
            // ARRANGE: SetControlItem (тип 0) + RFFilter (код 0x0044) + Body (0x01)
            // Header (Длина 5): 5 << 13 = 40960 (0xA000). 40960 + 5 = 40965 (0xA005). 
            // Якщо тип 0, то Header = 0x0005 (0x05, 0x00)
            // ВИПРАВЛЕНО: Тип 0, довжина 5 (3 body + 2 header). 5 + (0 << 13) = 5 (0x0005).
            var msg = new byte[] { 0x05, 0x00, 0x44, 0x00, 0x01 }; // 0x05, 0x00, 0x44, 0x00, 0x01
            
            // ACT
            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);
            
            // ASSERT
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(MsgTypes.SetControlItem));
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.RFFilter));
            Assert.That(body, Is.EqualTo(new byte[] { 0x01 }));
        }

        [Test]
        public void TranslateMessage_ControlItem_Failure_InvalidCode()
        {
            // ARRANGE: Неіснуючий код елемента (0x9999)
            // ВИПРАВЛЕНО: Header (довжина 5). 0x05, 0x00
            var msg = new byte[] { 0x05, 0x00, 0x99, 0x99, 0x01 }; 

            // ACT
            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);

            // ASSERT: Должно вернуть false, т.к. код элемента не определен
            Assert.That(success, Is.False);
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.None)); 
        }

        [Test]
        public void TranslateMessage_DataItem_Success_SequenceNumber()
        {
            // ARRANGE: DataItem0 (тип 4) + Sequence (0x1234) + Body (0xAA)
            // ВИПРАВЛЕНО: Тип 4, довжина 5. 5 + (4 << 13) = 32773 (0x8005). Байты: 0x05, 0x80.
            var msg = new byte[] { 0x05, 0x80, 0x34, 0x12, 0xAA }; // 0x05, 0x80, 0x34, 0x12, 0xAA

            // ACT
            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);

            // ASSERT
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(MsgTypes.DataItem0));
            Assert.That(seq, Is.EqualTo(0x1234));
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.None));
            Assert.That(body, Is.EqualTo(new byte[] { 0xAA }));
        }

        // ... (решта методів, включаючи GetSamples) ...
    }
}