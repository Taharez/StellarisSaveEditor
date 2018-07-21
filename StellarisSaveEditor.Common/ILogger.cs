using System;
using System.Threading.Tasks;

namespace StellarisSaveEditor.Common
{
    public interface ILogger : IDisposable
    {
        void Log(LogLevel level, string message);

        Task SaveAsync();
    }
}
