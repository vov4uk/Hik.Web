using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Hik.Api;
using Hik.Client.Abstraction.Services;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;
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
            try
            {
                var result = await RunAsync(config, from, to);
                return Success(result);
            }
            catch (HikException ex)
            {
                var msg = $"Code : {ex.ErrorCode}; {ex.ErrorMessage}";
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", msg, ex.ToStringDemystified());
                return Failure<IReadOnlyCollection<MediaFileDto>>(msg);
            }
            catch (Exception e)
            {
                Log.Error("ErrorMsg: {errorMsg}; Trace: {trace}", e.Message, e.ToStringDemystified());
                return Failure<IReadOnlyCollection<MediaFileDto>>(e.Message);
            }
        }

        protected abstract Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to);
    }
}
