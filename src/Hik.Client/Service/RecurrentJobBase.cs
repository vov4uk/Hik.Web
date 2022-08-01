﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using static CSharpFunctionalExtensions.Result;

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

        public async Task<Result<IReadOnlyCollection<MediaFileDto>>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            return await FirstFailureOrSuccess(
                FailureIf(config == null, "Invalid config"),
                FailureIf(!this.directoryHelper.DirExist(config?.DestinationFolder), $"DestinationFolder doesn't exist: {config?.DestinationFolder}"))
                .OnSuccessTry<IReadOnlyCollection<MediaFileDto>>(async () =>
                {
                    return await RunAsync(config, from, to);
                });
        }

        protected abstract Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to);
    }
}
