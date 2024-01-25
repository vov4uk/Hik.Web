using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Job.Email;
using Serilog;

namespace Job.Impl
{
    public class ImagesCollectorJob : CollectorBaseClass<ImagesCollectorConfig>
    {
        public ImagesCollectorJob(JobTrigger trigger,
            IImagesCollectorService worker,
            IHikDatabase db,
            IEmailHelper email,
            ILogger logger)
            : base(trigger, worker, db, email, logger)
        {
            this.configValidator = new ImagesConfigValidator();
        }
    }
}