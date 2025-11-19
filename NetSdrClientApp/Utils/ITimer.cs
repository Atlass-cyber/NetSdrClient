using System;
using System.Threading; 

namespace NetSdrClientApp.Utils 
{
    public interface ITimer : IDisposable
    {
        void Start(int intervalMilliseconds, TimerCallback callback, object? state);
    }
}