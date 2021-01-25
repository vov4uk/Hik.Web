using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using NLog;

namespace Hik.Client.Service
{
    public abstract class RecurrentJobBase<T> : IRecurrentJob<T>
    {
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public abstract Task<IReadOnlyCollection<T>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to);

        protected virtual void OnExceptionFired(ExceptionEventArgs e, BaseConfig config)
        {
            if (ExceptionFired != null)
            {
                ExceptionFired?.Invoke(this, e);
            }
            else
            {
                logger.Error(e.Exception, $"{config.Alias} - {e.Exception.Message}");
            }
        }
    }
}
