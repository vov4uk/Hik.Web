using System;
using System.Net;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Microsoft.Extensions.Logging;

namespace Hik.Client.Client
{
    public abstract class FtpClientBase : IClientBase
    {
        protected readonly DeviceConfig deviceConfig;
        protected readonly IFtpClient ftp;
        protected readonly ILogger logger;
        private bool disposedValue = false;

        protected FtpClientBase(
            DeviceConfig config,
            IFtpClient ftp,
            ILogger logger)
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
            ftp.ConnectTimeout = 5 * 1000;
            ftp.DataConnectionReadTimeout = 5 * 1000;
            ftp.ReadTimeout = 5 * 1000;
            ftp.DataConnectionConnectTimeout = 5 * 1000;
            ftp.RetryAttempts = 3;
            ftp.Credentials = new NetworkCredential(deviceConfig.UserName, deviceConfig.Password);
        }

        public bool Login()
        {
            ftp.Connect();
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
                logger.LogDebug("Logout the device");

                ftp?.Disconnect();
                ftp?.Dispose();

                disposedValue = true;
            }
        }
    }
}
