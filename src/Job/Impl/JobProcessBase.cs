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
        protected readonly Logger Logger;
        public string TriggerKey { get; private set; }
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }

        public Parameters Parameters;

        public abstract JobType JobType { get; }

        public abstract Task<JobResult> Run();

        public JobProcessBase(string trigger, string path, string connectionString)
        {
            TriggerKey = trigger;
            ConfigPath = path;
            ConnectionString = connectionString;
            this.Logger = LogManager.GetLogger(TriggerKey);
        }

        public async Task ExecuteAsync()
        {
            var job = new HikJob
            {
                Started = DateTime.Now,
                JobType = TriggerKey,
            };

            var unitOfWorkFactory = new UnitOfWorkFactory(ConnectionString);

            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.Add(job);
                await unitOfWork.SaveChangesAsync();
            }

            var result = await Run();

            job.Finished = DateTime.Now;
            Logger.Info("Save to DB...");
            var jobResultSaver = new JobService(unitOfWorkFactory, job, result);
            await jobResultSaver.SaveAsync();
            Logger.Info("Save to DB. Done");

            Logger.Info($"{JobType} Execution finished");
        }

    }
}
