using System.Reflection; // Потрібен для роботи зі збірками (Assemblies)
using NetArchTest.Rules; // Головна бібліотека для архітектурних тестів
using NUnit.Framework; // Використовуємо NUnit, оскільки він вже є у проекті
using NetSdrClientApp.Networking; // Додано, щоб знайти клас TcpClientWrapper

// using NUnit.Framework.Legacy; - Більше не потрібен

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // 1. Завантажуємо збірку (проект) клієнта для аналізу.
        private static readonly Assembly ClientAppAssembly = typeof(TcpClientWrapper).Assembly;


        [Test] // Атрибут для NUnit (замість [Fact] для Xunit)
        public void ClientApp_Should_Not_Depend_On_Server()
        {
            // --- Етап "Arrange" (Підготовка) ---
            
            var result = Types.InAssembly(ClientAppAssembly) // Взяти всі типи у збірці "ClientAppAssembly"
                .ShouldNot() // Вони НЕ ПОВИННІ
                .HaveDependencyOn("EchoServer") // Мати залежність від збірки "EchoServer" (за іменем)
                .GetResult(); // Отримати результат перевірки

            // --- Етап "Assert" (Перевірка) ---
            
            // 3. Перевіряємо результат, використовуючи NUnit 4 синтаксис "Constraint Model"
            Assert.That(result.IsSuccessful, Is.True,
                "Клієнтський додаток (NetSdrClientApp) не повинен залежати від сервера (EchoServer).");
        }
    }
}