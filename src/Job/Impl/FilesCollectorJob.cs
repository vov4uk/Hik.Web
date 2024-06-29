using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.Helpers.Email;
using Serilog;
namespace Job.Impl
{
    public class FilesCollectorJob : CollectorBaseClass<FilesCollectorConfig>
    {
        public FilesCollectorJob(JobTrigger trigger,
            IFilesCollectorService worker,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, worker, db, email, logger)
        {
            this.configValidator = new FilesConfigValidator();
        }
    }
}