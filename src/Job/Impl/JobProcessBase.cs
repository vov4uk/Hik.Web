using CSharpFunctionalExtensions;
using FluentValidation;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Job.Impl
{
    public abstract class JobProcessBase<T> : IJobProcess
        where T : BaseConfig
    {
        protected readonly ILogger logger;
        protected readonly IHikDatabase db;
        protected readonly IEmailHelper email;
        protected JobTrigger jobTrigger;
        protected AbstractValidator<T> configValidator;

        protected JobProcessBase(JobTrigger trigger, IHikDatabase db, IEmailHelper email, ILogger logger)
        {
            jobTrigger = trigger;
            this.logger = logger;
            this.db = db;
            this.email = email;
            Config = HikConfigExtensions.GetConfig<T>(trigger.Config) ?? throw new ArgumentNullException(nameof(Config));
            logger.Information("Config {config}", Config.ToString());
        }

        public T Config { get; protected set; }

        public HikJob JobInstance { get; private set; }

        public async Task ExecuteAsync()
        {
            try
            {
                JobInstance = db.CreateJob(GetHikJob());

                jobTrigger.LastExecutedJob = JobInstance;
                db.UpdateJobTrigger(jobTrigger);

                if (!Directory.Exists(Config.DestinationFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(Config.DestinationFolder);
                    }
                    catch (IOException)
                    {
                        //OK
                    }
                }

                this.configValidator.ValidateAndThrow(Config);

                var result = await RunAsync();
                await SaveResultsInternal(result);
            }
            catch (Exception e)
            {
                logger.Error("ExecuteAsync: {errorMsg}; Trace: {trace}", GetFullMessage(e), e.ToStringDemystified());
                HandleError(e);
            }
        }

        protected abstract Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync();

        protected virtual HikJob GetHikJob()
        {
            return new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = jobTrigger.Id,
                Success = true
            };
        }

        protected virtual async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.FilesCount = files.Count;
            db.SaveFiles(JobInstance, files);
            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
        }

        protected void HandleError(Exception e)
        {
            HandleError(GetFullMessage(e));
        }

        protected void HandleError(string error)
        {
            try
            {
                this.JobInstance.Finished = DateTime.Now;
                this.JobInstance.Success = false;

                db.LogExceptionTo(JobInstance.Id, error);
                db.UpdateJob(JobInstance);
            }
            catch (Exception e)
            {
                logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", $"Failed to save error : {error}", e.ToStringDemystified());
            }

            if (jobTrigger.SentEmailOnError)
            {
                var details = JobInstance?.ToHtmlTable(Config);
                email.Send(error, jobTrigger.TriggerKey , details);
            }
        }

        private async Task SaveResultsInternal(Result<IReadOnlyCollection<MediaFileDto>> result)
        {
            if (result.IsSuccess)
            {
                if (result.Value?.Count > 0)
                {
                    await SaveResultsAsync(result.Value);
                }
                else
                {
                    logger.Information("Results empty");
                }

                this.JobInstance.Finished = DateTime.Now;
                db.UpdateJob(JobInstance);

                jobTrigger.LastSync = JobInstance.LatestFileEndDate ?? JobInstance.Started;
                db.UpdateJobTrigger(jobTrigger);
            }
            else
            {
                HandleError(result.Error);
            }
        }

        public static string GetFullMessage(Exception ex)
        {
            return ex.InnerException == null
                 ? ex.Message
                 : ex.Message + " --> " + GetFullMessage(ex.InnerException);
        }
    }
}