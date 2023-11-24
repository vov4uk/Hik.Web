using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Serilog;

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

        public ClientFactory(
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            IImageHelper imageHelper)
        {
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.mapper = mapper;
            this.imageHelper = imageHelper;
        }

        public IDownloaderClient Create(CameraConfig camera, ILogger logger)
        {
            switch (camera.ClientType)
            {
                case ClientType.HikVisionVideo:
                    return new HikVideoClient(camera, this.hikApi, this.filesHelper, this.directoryHelper, this.mapper, logger);
                case ClientType.HikVisionPhoto:
                    return new HikPhotoClient(camera, this.hikApi, this.filesHelper, this.directoryHelper, this.mapper, this.imageHelper, logger);
                case ClientType.Yi:
                case ClientType.Yi720p:
                    return new YiClient(camera, this.filesHelper, this.directoryHelper, new FluentFTP.AsyncFtpClient(), logger);
                case ClientType.FTPDownload:
                    return new FtpDownloaderClient(camera, this.filesHelper, this.directoryHelper, new FluentFTP.AsyncFtpClient(), logger);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
