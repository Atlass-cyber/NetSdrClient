using System;
using System.Threading;
using NetSdrClientApp.Utils; 

namespace NetSdrClientApp.Utils
{
    public class SystemTimerWrapper : ITimer
    {
        private Timer? _timer;

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

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}