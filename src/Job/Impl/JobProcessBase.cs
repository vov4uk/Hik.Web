using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Api;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
using Job.Email;
using NLog;
using Hik.Client.Events;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        private AppConfig appConfig;
        protected HikJob JobInstance { get; private set; }

        protected readonly UnitOfWorkFactory UnitOfWorkFactory;

        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public string TriggerKey { get; private set; }
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }

        public AppConfig AppConfig => appConfig ?? (appConfig = HikConfig.GetConfig(this.ConfigPath));

        public Parameters Parameters;

        public abstract JobType JobType { get; }

        public abstract Task InitializeProcessingPeriod();

        public abstract Task<IReadOnlyCollection<MediaFileBase>> Run();

        public abstract Task SaveResults(IReadOnlyCollection<MediaFileBase> files, JobService service);

        public JobProcessBase(string trigger, string configFilePath, string connectionString, Guid activityId)
        {
            TriggerKey = trigger;
            ConfigPath = configFilePath;
            ConnectionString = connectionString;
            System.Diagnostics.Trace.CorrelationManager.ActivityId = activityId;
            this.UnitOfWorkFactory = new UnitOfWorkFactory(ConnectionString);
        }

        public async Task ExecuteAsync()
        {
            await InitializeJobInstance();

            var result = await Run();
            await SaveResultsInternal(result);

            Logger.Info($"{JobType} Execution finished");
        }

        private async Task InitializeJobInstance()
        {
            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobType = TriggerKey,
            };

            await InitializeProcessingPeriod();

            await SaveJobInstanceToDB();
        }

        private async Task SaveJobInstanceToDB()
        {
            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.AddAsync(JobInstance);
                await unitOfWork.SaveChangesAsync();
            }
        }


        protected virtual void ExceptionFired(object sender, ExceptionEventArgs e)
        {
            this.JobInstance.Success = false;
            Logger.Error(e.Exception, e.Exception.Message);            
            LogExceptionToDB(e);
            EmailHelper.Send(e.Exception);
        }

        private void LogExceptionToDB(ExceptionEventArgs e)
        {
            try
            {
                using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
                {
                    var jobRepo = unitOfWork.GetRepository<ExceptionLog>();
                    jobRepo.AddAsync(new ExceptionLog
                    {
                        CallStack = e.Exception.StackTrace,
                        JobId = JobInstance.Id,
                        Message = (e.Exception as HikException)?.ErrorMessage ?? e.Exception.Message,
                        HikErrorCode = (e.Exception as HikException)?.ErrorCode
                    }).GetAwaiter().GetResult();
                    unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex) { Logger.Error(ex, ex.Message); }
        }

        public async Task SaveResultsInternal(IReadOnlyCollection<MediaFileBase> files)
        {
            Logger.Info("Save to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, this.JobInstance);
            if (files.Any())
            {
                await SaveResults(files, jobResultSaver);
            }
            await jobResultSaver.SaveJobResultAsync(AppConfig.Camera);
            Logger.Info("Save to DB. Done");
        }
    }
}
