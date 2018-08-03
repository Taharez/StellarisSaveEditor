using System;
using Windows.Foundation.Diagnostics;
using StellarisSaveEditor.Common;
using System.Threading.Tasks;

namespace StellarisSaveEditor.Helpers
{
    public class UwpLogger : ILogger
    {
        private readonly LoggingChannel _channel;
        private readonly FileLoggingSession _session;

        public UwpLogger()
        {
            _session = new FileLoggingSession("session");
            _channel = new LoggingChannel("channel", null);
            _session.AddLoggingChannel(_channel);
        }

        public void Dispose()
        {
            _session.Dispose();
        }

        public void Log(LogLevel level, string message)
        {
            _channel.LogMessage(message, GetUwpLogLevel(level));
        }

        public async Task SaveAsync()
        {
            await _session.CloseAndSaveToFileAsync();
        }

        private LoggingLevel GetUwpLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    return LoggingLevel.Critical;
                case LogLevel.Error:
                    return LoggingLevel.Error;
                case LogLevel.Warning:
                    return LoggingLevel.Warning;
                case LogLevel.Verbose:
                    return LoggingLevel.Verbose;
                default:
                    return LoggingLevel.Information;
            }
        }
    }
}
