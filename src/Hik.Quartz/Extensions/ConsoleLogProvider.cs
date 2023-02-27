using Microsoft.Extensions.Logging;
using Quartz.Logging;
using System;
using LogLevel = Quartz.Logging.LogLevel;

namespace Hik.Quartz.Extensions
{
    public class ConsoleLogProvider : ILogProvider
    {
        public Logger GetLogger(string name)
        {
            var logger = new LoggerFactory()
                .AddFile($"logs\\Quartz.txt")
                .CreateLogger("Quartz");

            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Trace && func != null)
                {
                    logger.Log(GetMicrosoftLogLevel(level), func(), parameters);
                }
                return true;
            };
        }

        private Microsoft.Extensions.Logging.LogLevel GetMicrosoftLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;
                case LogLevel.Debug:
                    return Microsoft.Extensions.Logging.LogLevel.Debug;
                case LogLevel.Info:
                    return Microsoft.Extensions.Logging.LogLevel.Information;
                case LogLevel.Warn:
                    return Microsoft.Extensions.Logging.LogLevel.Warning;
                case LogLevel.Error:
                    return Microsoft.Extensions.Logging.LogLevel.Error;
                case LogLevel.Fatal:
                default:
                    return Microsoft.Extensions.Logging.LogLevel.Critical;
            }
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }
    }
}
