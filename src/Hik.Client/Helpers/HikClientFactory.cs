namespace Hik.Client.Helpers
{
    using System.Diagnostics.CodeAnalysis;
    using Hik.Api.Abstraction;
    using Hik.Client;
    using Hik.Client.Abstraction;
    using Hik.DTO.Config;
    using NLog;

    [ExcludeFromCodeCoverage]
    public class HikClientFactory : IHikClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public HikClientFactory(IHikApi hikApi, IFilesHelper filesHelper)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
        }

        public IHikClient Create(CameraConfig camera)
        {
            return new HikClient(camera, this.hikApi, this.filesHelper, this.logger);
        }
    }
}
