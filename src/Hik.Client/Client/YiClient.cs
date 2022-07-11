using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Client;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;

namespace Hik.Client
{
    public class YiClient : FtpBaseClient
    {
        private const string YiFileNameFormat = "mm'M00S'";

        public YiClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
            : base(config, filesHelper, directoryHelper, ftp)
        {
        }

        public override Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<MediaFileDto>();
            periodStart = new DateTime(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, 0, 0);
            var end = periodEnd.AddMinutes(-1);

            while (periodStart < end)
            {
                var file = new MediaFileDto
                {
                    Name = periodStart.ToUniversalTime().ToString(YiFileNameFormat),
                    Date = periodStart,
                    Duration = 60,
                };
                result.Add(file);
                periodStart = periodStart.AddMinutes(1);
            }

            return Task.FromResult(result.AsReadOnly() as IReadOnlyCollection<MediaFileDto>);
        }

        protected override string GetRemoteFilePath(MediaFileDto remoteFile)
            => remoteFile.Date.ToYiFilePathString(config.ClientType);

        protected override string GetLocalFilePath(MediaFileDto remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            directoryHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, remoteFile.ToYiFileNameString());
        }

        protected override Task<bool> PostDownloadFileProcessAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            remoteFile.Size = filesHelper.FileSize(localFilePath);
            remoteFile.Path = localFilePath;
            return Task.FromResult(true);
        }
    }
}
