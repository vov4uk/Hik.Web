using System;
using NLog;
using ILog = HikConsole.Abstraction.ILogger;

namespace HikConsole.Helpers
{
    public class Logger : ILog
    {
        private readonly NLog.Logger logger;

        public Logger()
        {
            this.logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        ///     Gets logger, not very nice code, but we need this for logging inside of static methods.
        /// </summary>
        public static Logger Instance => new Logger();

        public void Trace(string message)
        {
            this.Log(LogLevel.Trace, message);
        }

        public void Debug(string message)
        {
            this.Log(LogLevel.Debug, message);
        }

        public void Error(string message)
        {
            this.Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            this.LogErrorWithInnerExceptions(message, exception);
        }

        public void Info(string message)
        {
            this.Log(LogLevel.Info, message);
        }

        public void Warn(string message)
        {
            this.Log(LogLevel.Warn, message);
        }

        private void Log(
            LogLevel level,
            string message,
            Exception ex = null)
        {
            var logEventInfo = new LogEventInfo
            {
                Level = level,
                Exception = ex,
                LoggerName = this.logger.Name,
                Message = message,
            };

            this.logger.Log(logEventInfo);
        }

        private void LogErrorWithInnerExceptions(string error, Exception exception)
        {
            if (exception != null)
            {
                this.Log(LogLevel.Error, error, exception);

                AggregateException aggregateException = exception as AggregateException;

                if (aggregateException != null)
                {
                    for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                    {
                        string formattedError = $"{i.ToString()} inner exception for: {error}.";
                        this.LogErrorWithInnerExceptions(formattedError, aggregateException.InnerExceptions[i]);
                    }
                }

                this.LogErrorWithInnerExceptions(error, exception.InnerException);
            }
        }
    }
}
