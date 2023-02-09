using CSharpFunctionalExtensions;
using FluentValidation;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers;
using Job.Email;
using Job.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        protected JobProcessBase(string trigger, T config, IHikDatabase db, IEmailHelper email, ILogger logger)
        {
            TriggerKey = trigger;
            this.logger = logger;
            this.db = db;
            this.email = email;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            logger.LogInformation("Config {config}", Config.ToString());
        }

        public T Config { get; protected set; }
        public string TriggerKey { get; private set; }
        internal HikJob JobInstance { get; private set; }

        public async Task ExecuteAsync()
        {
            try
            {
                await CreateJobInstanceAsync();
                Config.Alias = TriggerKey;

                if (!Directory.Exists(Config.DestinationFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(Config.DestinationFolder);
                    }
                    catch (IOException)
                    {
                    }
                }

                this.configValidator.ValidateAndThrow(Config);

                var result = await RunAsync();
                await SaveResultsInternalAsync(result);
            }
            catch (Exception e)
            {
                HandleError(e.Message);
                logger.LogError(e, "Something went wrong");
            }
        }

        protected abstract Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync();

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
                this.JobInstance.Success = false;
                Task.WaitAll(
                    db.LogExceptionToAsync(JobInstance.Id, error),
                    db.SaveJobResultAsync(JobInstance));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save error");
            }

            if (Config.SentEmailOnError)
            {
                var details = JobInstance.ToHtmlTable(Config);
                email.Send(error, TriggerKey, details);
            }
        }

        private async Task CreateJobInstanceAsync()
        {
            this.jobTrigger = await db.GetOrCreateJobTriggerAsync(TriggerKey);

            JobInstance = await db.CreateJobInstanceAsync(new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = jobTrigger.Id,
                Success = true
            });
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
                    logger.LogWarning("Results empty");
                }

                await db.SaveJobResultAsync(JobInstance);
            }
            else
            {
                HandleError(result.Error);
            }
        }
    }
}