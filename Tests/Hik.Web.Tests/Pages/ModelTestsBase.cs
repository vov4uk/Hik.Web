using MediatR;
using Moq;

namespace Hik.Web.Tests.Pages
{
    public abstract class ModelTestsBase
    {
        protected const string group = "JobHost";
        protected const string name = "Floor1_Video";
        protected const string videoJob = "Job.Impl.HikVideoDownloaderJob, Job";
        protected const string photoJob = "Job.Impl.HikPhotoDownloaderJob, Job";
        protected const string archiveJob = "Job.Impl.ArchiveJob, Job";
        protected readonly Mock<IMediator> _mediator = new (MockBehavior.Strict);
    }
}
