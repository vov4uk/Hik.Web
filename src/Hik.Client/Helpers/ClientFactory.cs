namespace Hik.Client.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using AutoMapper;
    using Hik.Api.Abstraction;
    using Hik.Client;
    using Hik.Client.Abstraction;
    using Hik.DTO.Config;

    [ExcludeFromCodeCoverage]
    public class ClientFactory : IClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly IMapper mapper;

        public ClientFactory(IHikApi hikApi, IFilesHelper filesHelper, IMapper mapper)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.mapper = mapper;
        }

        public IClient Create(CameraConfig camera)
        {
            switch (camera.ClientType)
            {
                case ClientType.HikVision:
                    return new HikVideoClient(camera, this.hikApi, this.filesHelper, this.mapper);
                case ClientType.Yi:
                case ClientType.Yi720p:
                    return new YiClient(camera, this.filesHelper);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
