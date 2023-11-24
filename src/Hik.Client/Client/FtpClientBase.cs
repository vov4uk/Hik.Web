using System;
using System.Net;
using FluentFTP;
using FluentFTP.Logging;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Microsoft.Extensions.Logging;
using Logger = Serilog.ILogger;

namespace Hik.Client.Client
{
    public abstract class FtpClientBase : IClientBase
    {
        protected readonly DeviceConfig deviceConfig;
        protected readonly IAsyncFtpClient ftp;
        protected readonly Logger logger;
        private bool disposedValue = false;

        protected FtpClientBase(
            DeviceConfig config,
            IAsyncFtpClient ftp,
            Logger logger)
        {
            this.deviceConfig = config ?? throw new ArgumentNullException(nameof(config));
            this.ftp = ftp;
            this.logger = logger;
        }

        public void ForceExit()
        {
            Dispose(true);
        }

        public void InitializeClient()
        {
            ftp.Host = deviceConfig.IpAddress;
            ftp.Port = deviceConfig.PortNumber;
            ftp.Config.ConnectTimeout = 15 * 1000;
            ftp.Config.DataConnectionReadTimeout = 15 * 1000;
            ftp.Config.ReadTimeout = 15 * 1000;
            ftp.Config.DataConnectionConnectTimeout = 15 * 1000;
            ftp.Config.RetryAttempts = 5;
            ftp.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
            ftp.Credentials = new NetworkCredential(deviceConfig.UserName, deviceConfig.Password);
#if DEBUG
            ILogger logger;

            using (var factory = new LoggerFactory())
            {
                logger = factory.AddFile($"logs\\{deviceConfig.IpAddress}.txt")
                    .CreateLogger(deviceConfig.IpAddress);
            }

            ftp.Logger = new FtpLogAdapter(logger);
#endif
        }

        public bool Login()
        {
            var profile = ftp.AutoConnect().GetAwaiter().GetResult();
            ftp.Connect(profile).GetAwaiter().GetResult();
            logger.Information("Successfully logged to {IpAddress}", deviceConfig.IpAddress);
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                logger.Debug("Logout the device");

                ftp?.Disconnect();
                ftp?.Dispose();

                disposedValue = true;
            }
        }
    }
}
