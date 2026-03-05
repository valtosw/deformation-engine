using Microsoft.Extensions.Logging;

namespace Logging
{
    internal static class LoggingFactory
    {
        private static ILoggerFactory? _loggerFactory;

        public static ILoggerFactory Create()
        {
            if (_loggerFactory is not null)
                return _loggerFactory;

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole()
                    .AddDebug();
            });

            return _loggerFactory;
        }
    }
}
