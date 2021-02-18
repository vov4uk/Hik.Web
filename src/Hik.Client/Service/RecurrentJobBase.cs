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
            this.logger.Info("Start ArchiveService");
            if (!this.directoryHelper.DirectoryExists(config.DestinationFolder))
            {
                this.logger.Error($"Output doesn't exist: {config.DestinationFolder}");
                return default;
            }

            return await RunAsync(config, from, to);
        }

        protected abstract Task<IReadOnlyCollection<T>> RunAsync(BaseConfig config, DateTime from, DateTime to);

        protected virtual void OnExceptionFired(ExceptionEventArgs e, BaseConfig config)
        {
            if (ExceptionFired != null)
            {
                ExceptionFired?.Invoke(this, e);
            }
            else
            {
                logger.Error(e.ToString());
            }
        }
    }
}
