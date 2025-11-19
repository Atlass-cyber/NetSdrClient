using System;
using System.Threading;
using NetSdrClientApp.Utils; 

namespace NetSdrClientApp.Utils
{
    public class SystemTimerWrapper : ITimer
    {
        private Timer? _timer;
        private bool disposedValue; // ДОБАВЛЕНО

        public void Start(int intervalMilliseconds, TimerCallback callback, object? state)
        {
            if (_timer != null)
                throw new InvalidOperationException("Timer is already running.");
            
            _timer = new Timer(
                callback,
                state, 
                0, 
                intervalMilliseconds);
        }

        // ИЗМЕНЕНО: Полный паттерн Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timer?.Dispose(); // Очистка управляемого ресурса
                }
                _timer = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}