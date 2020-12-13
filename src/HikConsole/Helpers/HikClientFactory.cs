using System.Diagnostics.CodeAnalysis;
using HikApi.Abstraction;
using HikConsole.Abstraction;
using HikConsole.DTO.Config;
using NLog;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class HikClientFactory : IHikClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly IProgressBarFactory progressFactory;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public HikClientFactory(IHikApi hikApi, IFilesHelper filesHelper, IProgressBarFactory progressFactory)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.progressFactory = progressFactory;
        }

        public IHikClient Create(CameraConfig camera)
        {
            return new HikClient(camera, this.hikApi, this.filesHelper, this.progressFactory, this.logger);
        }
    }
}
