using System.Diagnostics.CodeAnalysis;
using HikApi.Abstraction;
using HikConsole.Abstraction;
using HikConsole.Config;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class HikClientFactory : IHikClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly IProgressBarFactory progressFactory;
        private readonly ILogger logger;

        public HikClientFactory(IHikApi hikApi, IFilesHelper filesHelper, IProgressBarFactory progressFactory, ILogger logger)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.progressFactory = progressFactory;
            this.logger = logger;
        }

        public IHikClient Create(CameraConfig camera)
        {
            return new HikClient(camera, this.hikApi, this.filesHelper, this.progressFactory, this.logger);
        }
    }
}
