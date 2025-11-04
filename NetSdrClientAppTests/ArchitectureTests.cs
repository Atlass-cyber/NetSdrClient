using System.Reflection; // Потрібен для роботи зі збірками (Assemblies)
using NetArchTest.Rules; // Головна бібліотека для архітектурних тестів
using NUnit.Framework; // Використовуємо NUnit, оскільки він вже є у проекті
using NetSdrClientApp.Networking; // Додано, щоб знайти клас TcpClientWrapper

// using для сервера не потрібен, оскільки ми лише перевіряємо залежність за іменем

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // 1. Завантажуємо збірку (проект) клієнта для аналізу.
        // Це найнадійніший спосіб, він працює напряму через типи.
        private static readonly Assembly ClientAppAssembly = typeof(TcpClientWrapper).Assembly;

        // Посилання на збірку сервера (typeof(Program)) видалено,
        // оскільки метод .HaveDependencyOn() приймає рядок (string),
        // а клас Program, скоріш за все, не є public.

        [Test] // Атрибут для NUnit (замість [Fact] для Xunit)
        public void ClientApp_Should_Not_Depend_On_Server()
        {
            // --- Етап "Arrange" (Підготовка) ---
            
            // 2. Описуємо правило:
            var result = Types.InAssembly(ClientAppAssembly) // Взяти всі типи у збірці "ClientAppAssembly"
                .ShouldNot() // Вони НЕ ПОВИННІ
                .HaveDependencyOn("EchoServer") // Мати залежність від збірки "EchoServer" (за іменем)
                .GetResult(); // Отримати результат перевірки

            // --- Етап "Assert" (Перевірка) ---
            
            // 3. Перевіряємо результат.
            Assert.IsTrue(result.IsSuccessful, // Метод NUnit (замість Assert.True)
                "Клієнтський додаток (NetSdrClientApp) не повинен залежати від сервера (EchoServer).");
        }
    }
}