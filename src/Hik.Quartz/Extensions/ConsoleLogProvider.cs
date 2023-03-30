using Serilog;
using Quartz.Logging;
using System;
using LogLevel = Quartz.Logging.LogLevel;

namespace Hik.Quartz.Extensions
{
    public class ConsoleLogProvider : ILogProvider
    {
        public Logger GetLogger(string name)
        {

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext() 
                .WriteTo.Console()
                .WriteTo.File($"Logs\\Quartz.txt")
                .CreateLogger();

            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Trace && func != null)
                {
                    logger.Write(GetMicrosoftLogLevel(level), func(), parameters);
                }
                return true;
            };
        }

        private Serilog.Events.LogEventLevel GetMicrosoftLogLevel(LogLevel s)
        {
            return s switch
            {
                LogLevel.Debug => Serilog.Events.LogEventLevel.Verbose,
                LogLevel.Info => Serilog.Events.LogEventLevel.Information,
                LogLevel.Warn => Serilog.Events.LogEventLevel.Warning,
                LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information,
            };
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
