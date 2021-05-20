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
        protected readonly IDirectoryHelper directoryHelper;

        protected RecurrentJobBase(IDirectoryHelper directoryHelper)
        {
            this.directoryHelper = directoryHelper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public async Task<IReadOnlyCollection<T>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            try
            {
                this.logger.Info("Start ExecuteAsync");
                if (!this.directoryHelper.DirExist(config?.DestinationFolder))
                {
                    throw new InvalidOperationException($"Output doesn't exist: {config?.DestinationFolder}");
                }

                return await RunAsync(config, from, to);
            }
            catch (Exception ex)
            {
                OnExceptionFired(new ExceptionEventArgs(ex), config);
                return default;
            }
        }

        protected abstract Task<IReadOnlyCollection<T>> RunAsync(BaseConfig config, DateTime from, DateTime to);

        protected virtual void OnExceptionFired(ExceptionEventArgs e, BaseConfig config)
        {
            ExceptionFired?.Invoke(this, e);
            logger.Error(e.ToString());
        }
    }
}
