using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Serilog;
using Polly;

namespace Hik.Client.Client
{
    public class FtpUploaderClient : FtpClientBase, IUploaderClient
    {
        public FtpUploaderClient(DeviceConfig config, IAsyncFtpClient ftp, ILogger logger)
            : base(config, ftp, logger)
        {
        }

        public async Task UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir)
        {
            await Policy
                   .Handle<FtpException>()
                   .Or<TimeoutException>()
                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(3))
                   .ExecuteAsync(() => ftp.UploadFiles(localPaths, remoteDir, FtpRemoteExists.Overwrite));
        }
    }
}
