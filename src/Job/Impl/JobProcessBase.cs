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
            var job = new HikJob
            {
                Started = DateTime.Now,
                JobType = TriggerKey,
            };

            using (var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.AddAsync(job);
                await unitOfWork.SaveChangesAsync();
            }

            var result = await Run();

            job.Finished = DateTime.Now;
            Logger.Info("Save to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, job, result);
            await jobResultSaver.SaveAsync();
            Logger.Info("Save to DB. Done");

            Logger.Info($"{JobType} Execution finished");
        }

    }
}
