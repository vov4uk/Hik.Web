using System;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Dahua.Api.Abstractions;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.Client.Client;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public class ClientFactory : IClientFactory
    {
        private readonly IHikSDK hikSDK;
        private readonly IDahuaSDK dahuaSDK;
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IImageHelper imageHelper;
        private readonly IMapper mapper;

        public ClientFactory(
            IHikSDK hikSDK,
            IDahuaSDK dahuaSDK,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            IImageHelper imageHelper)
        {
            this.hikSDK = hikSDK;
            this.dahuaSDK = dahuaSDK;
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
                    return new HikVideoClient(camera, this.hikSDK, this.filesHelper, this.directoryHelper, this.mapper, logger);
                case ClientType.DahuaVideo:
                    return new DahuaVideoClient(camera, this.dahuaSDK, this.filesHelper, this.directoryHelper, this.mapper, logger);
                case ClientType.HikVisionPhoto:
                    return new HikPhotoClient(camera, this.hikSDK, this.filesHelper, this.directoryHelper, this.mapper, this.imageHelper, logger);
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
