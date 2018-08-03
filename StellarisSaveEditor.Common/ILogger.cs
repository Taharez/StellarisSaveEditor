using System;

namespace StellarisSaveEditor.Common
{
    public interface ILogger : IDisposable
    {
        void Log(LogLevel level, string message);
    }
}
