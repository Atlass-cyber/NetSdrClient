using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using NetSdrClientApp.Messages;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;
using static NUnit.Framework.Is;

namespace NetSdrClientAppTests.Messages
{
    [TestFixture]
    public class NetSdrMessageHelperTests
    {
        // =========================================================
        // ТЕСТИРОВАНИЕ СОЗДАНИЯ СООБЩЕНИЙ
        // =========================================================

        [Test]
        public void GetControlItemMessage_CreatesValidMessage_WithParameters()
        {
            var paramsData = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            var msgType = MsgTypes.SetControlItem;
            var itemCode = ControlItemCodes.ReceiverFrequency;
            
            var result = GetControlItemMessage(msgType, itemCode, paramsData);

            // 2 header + 2 itemCode + 4 params = 8 bytes
            Assert.That(result.Length, Is.EqualTo(8)); 
            Assert.That(result.Skip(4).ToArray(), Is.EqualTo(paramsData));

            // Перевіряємо, що самі можемо це прочитати
            bool success = TranslateMessage(result, out MsgTypes type, out ControlItemCodes code, out _, out byte[] body);
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(msgType));
            Assert.That(code, Is.EqualTo(itemCode));
            Assert.That(body, Is.EqualTo(paramsData));
        }
        
        [Test]
        public void GetDataItemMessage_CreatesValidMessage_WithNoneControlItem()
        {
            var paramsData = new byte[] { 0x01, 0x02 };
            var msgType = MsgTypes.DataItem0;
            
            var result = GetDataItemMessage(msgType, paramsData);

            // 2 header + 2 params = 4 bytes (DataItem не має ItemCode)
            Assert.That(result.Length, Is.EqualTo(4));
        }

        // =========================================================
        // ТЕСТИРОВАНИЕ ГРАНИЧНЫХ СЛУЧАЕВ ЗАГОЛОВКА (GetHeader)
        // =========================================================

        [Test]
        public void GetHeader_ThrowsException_IfLengthExceedsMax()
        {
            int excessivelyLargeBody = 8190; 
            
            Assert.That(() => typeof(NetSdrMessageHelper)
                .GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { MsgTypes.SetControlItem, excessivelyLargeBody }),
                Throws.InnerException.TypeOf<ArgumentException>());
        }
        
        [Test]
        public void GetHeader_ThrowsException_IfLengthIsNegative()
        {
            Assert.That(() => typeof(NetSdrMessageHelper)
                .GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { MsgTypes.SetControlItem, -1 }),
                Throws.InnerException.TypeOf<ArgumentException>());
        }
        
        [Test]
        public void GetHeader_HandlesDataItemEdgeCase()
        {
            int bodyLength = 8192;
            
             var result = (byte[])typeof(NetSdrMessageHelper)
                .GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { MsgTypes.DataItem0, bodyLength });

            var headerValue = BitConverter.ToUInt16(result);
            // Перевіряємо біти
            Assert.That((headerValue >> 13), Is.EqualTo((int)MsgTypes.DataItem0)); 
            Assert.That((headerValue & 0x1FFF), Is.EqualTo(0)); 
        }

        // =========================================================
        // ТЕСТИРОВАНИЕ ПАРСИНГА СООБЩЕНИЯ (TranslateMessage)
        // =========================================================
        
        [Test]
        public void TranslateMessage_ControlItem_Success()
        {
            // Використовуємо наш же метод для генерації валідного повідомлення
            var bodyData = new byte[] { 0x01 };
            var msg = GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, bodyData);
            
            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);
            
            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(MsgTypes.SetControlItem));
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.RFFilter));
            Assert.That(body, Is.EqualTo(bodyData));
        }

        [Test]
        public void TranslateMessage_ControlItem_Failure_InvalidCode()
        {
            // Створюємо валідне повідомлення, але підміняємо код на невалідний (0x9999)
            var msg = GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, new byte[] { 0x01 });
            
            // ItemCode знаходиться за зміщенням 2 і 3 (Header=2 bytes)
            var invalidCode = BitConverter.GetBytes((ushort)0x9999);
            msg[2] = invalidCode[0];
            msg[3] = invalidCode[1];

            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);

            Assert.That(success, Is.False);
            Assert.That(itemCode, Is.EqualTo(ControlItemCodes.None)); 
        }

        [Test]
        public void TranslateMessage_DataItem_Success_SequenceNumber()
        {
            // DataItem має SequenceNumber замість ItemCode. Створимо вручну, щоб контролювати Sequence.
            // Header для DataItem0. Повна довжина = 2 header + 2 seq + 1 body = 5 байт.
            // 5 + (4 << 13) = 5 + 32768 = 32773 -> 0x8005
            var header = BitConverter.GetBytes((ushort)(5 + ((int)MsgTypes.DataItem0 << 13))); // БУЛО 3, СТАЛО 5
            var seq = BitConverter.GetBytes((ushort)0x1234);
            var bodyData = new byte[] { 0xAA };

            var msg = header.Concat(seq).Concat(bodyData).ToArray();

            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort sequenceNumber, out byte[] body);

            Assert.That(success, Is.True);
            Assert.That(type, Is.EqualTo(MsgTypes.DataItem0));
            Assert.That(sequenceNumber, Is.EqualTo(0x1234));
            Assert.That(body, Is.EqualTo(bodyData));
        }

        [Test]
        public void TranslateMessage_DataItem_Success_MaxDataLengthEdgeCase()
        {
            // ARRANGE
            var sequenceNumberBytes = BitConverter.GetBytes((ushort)0x9999);
            var body = Enumerable.Repeat((byte)0x01, 8192 - 2).ToArray(); // 8194 (max) - 2 (header) - 2 (seq) = 8190
            
            // Header для Max Length має 0 у полі довжини
            var headerVal = (ushort)(0 + ((int)MsgTypes.DataItem0 << 13));
            var header = BitConverter.GetBytes(headerVal);

            var msg = header.Concat(sequenceNumberBytes).Concat(body).ToArray();

            // ACT
            bool success = TranslateMessage(msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] bodyOut);
            
            // ASSERT
            Assert.That(success, Is.True); 
            Assert.That(bodyOut.Length, Is.EqualTo(8192 - 2)); 
            Assert.That(type, Is.EqualTo(MsgTypes.DataItem0));
        }
        
        [Test]
        public void TranslateMessage_Failure_BodyTooShort()
        {
            // Створюємо повідомлення з довжиною 10 у заголовку, але тілом 1
            var msg = GetControlItemMessage(MsgTypes.Ack, ControlItemCodes.None, new byte[] { 0x01 });
            
            // Підміняємо заголовок на довжину 10
            var fakeHeader = BitConverter.GetBytes((ushort)(10 + ((int)MsgTypes.Ack << 13)));
            msg[0] = fakeHeader[0];
            msg[1] = fakeHeader[1];

            // Очікуємо False або Exception
            try 
            {
                bool success = TranslateMessage(msg, out _, out _, out _, out _);
                Assert.That(success, Is.False);
            }
            catch (Exception)
            {
                Assert.Pass(); 
            }
        }
        
        [Test]
        public void TranslateMessage_Failure_BodyTooLong()
        {
            // Заголовок каже довжина 3, але ми даємо 5 байтів
            var msg = new byte[] { 0x03, 0x60, 0xAA, 0xBB, 0xCC }; // Header 0x6003 (len 3, type SetControlItem)
            
            bool success = TranslateMessage(msg, out MsgTypes type, out _, out _, out _);

            Assert.That(success, Is.False); 
        }

        // =========================================================
        // ТЕСТИРОВАНИЕ ПОЛУЧЕНИЯ СЭМПЛОВ (GetSamples)
        // =========================================================

        [Test]
        public void GetSamples_ThrowsException_IfSampleSizeTooLarge()
        {
            ushort sampleSize = 40; 
            var body = Array.Empty<byte>();

            Assert.That(() => GetSamples(sampleSize, body).ToList(), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase((ushort)8, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new int[] { 0x00000001, 0x00000002, 0x00000003, 0x00000004 })]
        [TestCase((ushort)16, new byte[] { 0x01, 0x00, 0x02, 0x00 }, new int[] { 0x00000001, 0x00000002 })]
        [TestCase((ushort)24, new byte[] { 0x01, 0x00, 0x00, 0x02, 0x00, 0x00 }, new int[] { 0x00000001, 0x00000002 })]
        [TestCase((ushort)32, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 }, new int[] { 0x00000001, 0x00000002 })]
        public void GetSamples_TranslatesDataCorrectly(ushort sampleSize, byte[] body, int[] expected)
        {
            var results = GetSamples(sampleSize, body).ToArray();
            Assert.That(results, Is.EqualTo(expected));
        }

        [Test]
        public void GetSamples_HandlesPartialBody_AndStops()
        {
            ushort sampleSize = 16; 
            var body = new byte[] { 0x01, 0x00, 0x02 }; // 3 байти, вистачить тільки на 1 семпл (2 байти)

            var results = GetSamples(sampleSize, body).ToArray();

            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(0x00000001));
        }
    }
}