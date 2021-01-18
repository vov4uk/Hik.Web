namespace Hik.Client.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Hik.Api.Abstraction;
    using Hik.Client;
    using Hik.Client.Abstraction;
    using Hik.DTO.Config;

    [ExcludeFromCodeCoverage]
    public class HikClientFactory : IHikClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;

        public HikClientFactory(IHikApi hikApi, IFilesHelper filesHelper)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
        }

        public IHikClient Create(CameraConfig camera)
        {
            switch (camera.ClientType)
            {
                case ClientType.HikVision:
                    return new HikClient(camera, this.hikApi, this.filesHelper);
                case ClientType.Yi:
                case ClientType.Yi720p:
                    return new YiClient(camera, this.filesHelper);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
