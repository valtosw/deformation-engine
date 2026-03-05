using Microsoft.Extensions.Logging;

namespace Logging
{
    public static class Log
    {
        private static readonly ILoggerFactory Factory = LoggingFactory.Create();

        public static ILogger Create<T>() => Factory.CreateLogger<T>();
    }
}
