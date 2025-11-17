namespace NetSdrClientApp.Server
{
    public static class EchoLogic
    {
        // "Чиста" функція: приймає буфер і кількість байт
        public static byte[] ProcessMessage(byte[] message, int bytesToProcess)
        {
            // логіка - просто "ехо".
            // Створюємо новий масив точного розміру, що прийшов
            byte[] response = new byte[bytesToProcess];
            
            // Копіюємо з вхідного буфера у вихідний
            Array.Copy(message, response, bytesToProcess);
            
            return response;
        }
    }
}
