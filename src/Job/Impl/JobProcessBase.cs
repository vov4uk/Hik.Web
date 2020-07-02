using System;
using System.Threading.Tasks;
using Autofac;
using HikConsole.Abstraction;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.DTO;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        protected static readonly ILogger Logger = Logger = AppBootstrapper.Container.Resolve<ILogger>();
        public string Description { get; private set; }
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }

        public Parameters Parameters;

        public abstract JobType JobType { get; }

        public abstract Task<JobResult> Run();

        public JobProcessBase(string description, string path, string connectionString)
        {
            Description = description;
            ConfigPath = path;
            ConnectionString = connectionString;
        }

        public async Task Execute()
        {
            var job = new HikJob
            {
                Started = DateTime.Now,
                JobType = JobType.ToString(),
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
