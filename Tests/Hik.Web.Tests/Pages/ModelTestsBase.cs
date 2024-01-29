using MediatR;
using Moq;

namespace Hik.Web.Tests.Pages
{
    public abstract class ModelTestsBase
    {
        protected const string group = "JobHost";

        protected const string name = "Floor1_Video";

        protected const string videoJob = "Job.Impl.VideoDownloaderJob, Job";

        protected const string photoJob = "Job.Impl.PhotoDownloaderJob, Job";

        protected const string filesCollectorJob = "Job.Impl.FilesCollectorJob, Job";

        protected readonly Mock<IMediator> _mediator = new (MockBehavior.Strict);
    }
}
