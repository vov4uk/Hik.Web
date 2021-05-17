using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.DTO.Config;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public class ClientFactory : IClientFactory
    {
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IImageHelper imageHelper;
        private readonly IMapper mapper;

        public ClientFactory(IHikApi hikApi, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IMapper mapper, IImageHelper imageHelper)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.mapper = mapper;
            this.imageHelper = imageHelper;
        }

        public IClient Create(CameraConfig camera)
        {
            switch (camera.ClientType)
            {
                case ClientType.HikVisionVideo:
                    return new HikVideoClient(camera, this.hikApi, this.filesHelper, this.directoryHelper, this.mapper);
                case ClientType.HikVisionPhoto:
                    return new HikPhotoClient(camera, this.hikApi, this.filesHelper, this.directoryHelper, this.mapper, this.imageHelper);
                case ClientType.Yi:
                case ClientType.Yi720p:
                    return new YiClient(camera, this.filesHelper, this.directoryHelper, new FluentFTP.FtpClient());
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
