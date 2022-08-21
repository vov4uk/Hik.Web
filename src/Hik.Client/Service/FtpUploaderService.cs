using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using MediaFiles = System.Collections.Generic.IReadOnlyCollection<Hik.DTO.Contracts.MediaFileDto>;

namespace Hik.Client.Service
{
    public class FtpUploaderService : RecurrentJobBase, IFtpUploaderService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IUploaderClient ftp;

        public FtpUploaderService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IUploaderClient ftp,
            ILogger logger)
            : base(directoryHelper, logger)
        {
            this.filesHelper = filesHelper;
            this.ftp = ftp;
        }

        protected override async Task<MediaFiles> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var fConfig = config as FtpUploaderConfig;
            var allFiles = this.directoryHelper.EnumerateFiles(fConfig.DestinationFolder, fConfig.AllowedFileExtentions);

            var result = new List<MediaFileDto>();
            if (allFiles.Any())
            {
                var compressedFiles = new List<string>();
                foreach (var filePath in allFiles)
                {
                    var compressed = filesHelper.CompressFile(filePath);
                    compressedFiles.Add(compressed);
                    filesHelper.DeleteFile(filePath);
                    string fileName = filesHelper.GetFileName(filePath);
                    result.Add(new MediaFileDto
                    {
                        Name = fileName,
                        Path = filePath,
                        Date = DateTime.Now,
                    });
                }

                await MoveToFtp(compressedFiles, fConfig.RemoteFolder);
            }

            return result;
        }

        private async Task MoveToFtp(List<string> compressedFiles, string remoteFolder)
        {
            ftp.InitializeClient();
            if (ftp.Login())
            {
                await ftp.UploadFilesAsync(compressedFiles, remoteFolder);
                ftp.ForceExit();
                foreach (var filePath in compressedFiles)
                {
                    filesHelper.DeleteFile(filePath);
                }
            }
            else
            {
                ftp.ForceExit();
                throw new InvalidOperationException("Unable to login to FTP");
            }
        }
    }
}
