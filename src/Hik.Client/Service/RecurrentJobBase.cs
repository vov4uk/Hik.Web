﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client.Service
{
    public abstract class RecurrentJobBase : IRecurrentJob
    {
        protected readonly ILogger logger;
        protected readonly IDirectoryHelper directoryHelper;

        protected RecurrentJobBase(IDirectoryHelper directoryHelper, ILogger logger)
        {
            this.directoryHelper = directoryHelper;
            this.logger = logger;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public async Task<IReadOnlyCollection<MediaFileDto>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            try
            {
                if (!this.directoryHelper.DirExist(config?.DestinationFolder))
                {
                    throw new InvalidOperationException($"Output doesn't exist: {config?.DestinationFolder}");
                }

                return await RunAsync(config, from, to);
            }
            catch (Exception ex)
            {
                OnExceptionFired(ex);
                return default;
            }
        }

        protected abstract Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to);

        protected virtual void OnExceptionFired(Exception ex)
        {
            if (ExceptionFired != null)
            {
                ExceptionFired.Invoke(this, new ExceptionEventArgs(ex));
            }
            else
            {
                logger.LogError(ex.ToString());
            }
        }
    }
}
