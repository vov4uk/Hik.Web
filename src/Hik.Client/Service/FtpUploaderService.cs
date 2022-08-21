using System;
using System.Collections.Generic;
using System.IO;
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
                foreach (var filePath in allFiles)
                {
                    try
                    {
                        filesHelper.CompressFile(filePath);
                        filesHelper.DeleteFile(filePath);
                    }
                    catch (IOException e)
                    {
                        logger.LogError(e, "Failed to process file");
                    }
                }

                var compressedFiles = this.directoryHelper.EnumerateFiles(fConfig.DestinationFolder, new[] { ".zip" });

                result.AddRange(compressedFiles.Select(x => new MediaFileDto
                {
                    Path = x,
                    Date = DateTime.Now,
                    Size = filesHelper.FileSize(x),
                    Name = filesHelper.GetFileName(x),
                }));

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
