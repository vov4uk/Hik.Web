using System;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.DTO;
using HikConsole.Scheduler;
using NLog;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        protected HikJob JobInstance { get; private set; }

        protected readonly UnitOfWorkFactory UnitOfWorkFactory;

        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public string TriggerKey { get; private set; }
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }

        public Parameters Parameters;

        public abstract JobType JobType { get; }

        public abstract Task<JobResult> Run();

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
            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobType = TriggerKey,
            };

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.AddAsync(JobInstance);
                await unitOfWork.SaveChangesAsync();
            }

            var result = await Run();

            JobInstance.Finished = DateTime.Now;
            Logger.Info("Save to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, JobInstance);
            await jobResultSaver.SaveJobResultAsync(result);
            Logger.Info("Save to DB. Done");

            Logger.Info($"{JobType} Execution finished");
        }

    }
}
