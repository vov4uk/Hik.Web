using CSharpFunctionalExtensions;
using FluentValidation;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var job = new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = jobTrigger.Id,
                Success = true
            };

            SetProcessingPeriod(job);

            JobInstance = await db.CreateJobAsync(job);

            jobTrigger.LastExecutedJob = JobInstance;
            await db.UpdateJobTriggerAsync(jobTrigger);

            try
            {
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
                await SaveResultsInternalAsync(result);
            }
            catch (DbUpdateException e)
            {
                logger.Error("Trigger {trigger} ErrorMsg: {errorMsg}; Trace: {trace}", jobTrigger.TriggerKey, $"DB error : {e.Message}", e.ToStringDemystified());
                if (jobTrigger.SentEmailOnError)
                {
                    email.Send(e.Message, jobTrigger.TriggerKey, null);
                }
            }
            catch (Exception e)
            {
                logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", e.Message, e.ToStringDemystified());
                HandleError(e.Message);
            }
        }

        protected abstract Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync();

        protected abstract void SetProcessingPeriod(HikJob job);

        protected virtual async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDto> files)
        {
            JobInstance.FilesCount = files.Count;
            var mediaFiles = await db.SaveFilesAsync(JobInstance, files);
            await db.UpdateDailyStatisticsAsync(jobTrigger.Id, files);
            await db.SaveDownloadHistoryFilesAsync(JobInstance, mediaFiles);
        }

        private void HandleError(string error)
        {
            try
            {
                this.JobInstance.Finished = DateTime.Now;
                this.JobInstance.Success = false;
                Task.WaitAll(
                    db.LogExceptionToAsync(JobInstance.Id, error),
                    db.UpdateJobAsync(JobInstance));
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

        private async Task SaveResultsInternalAsync(Result<IReadOnlyCollection<MediaFileDto>> result)
        {
            if (result.IsSuccess)
            {
                if (result.Value?.Any() == true)
                {
                    await SaveResultsAsync(result.Value);
                }
                else
                {
                    logger.Information("Results empty");
                }

                this.JobInstance.Finished = DateTime.Now;
                await db.UpdateJobAsync(JobInstance);

                jobTrigger.LastSync = JobInstance.LatestFileEndDate ?? JobInstance.Started;
                await db.UpdateJobTriggerAsync(jobTrigger);
            }
            else
            {
                HandleError(result.Error);
            }
        }
    }
}