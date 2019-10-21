using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Data;
using HikConsole.Helpers;
using C = HikConsole.Helpers.ConsoleHelper;

namespace HikConsole
{
    public class HikConsole
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;
        private readonly AppConfig appConfig;
        private readonly ISDKWrapper sdk;
        private int downloadHandle = -1;
        private FindResult downloadFile;
        private int userId = -1;
        private int channel = -1;
        private ProgressBar progress;

        public HikConsole(AppConfig appConfig, ISDKWrapper sdk)
        {
            this.appConfig = appConfig;
            this.sdk = sdk;
        }

        public bool IsDownloading => this.downloadHandle >= 0;

        public void Init()
        {
            this.sdk.Initialize();
            this.sdk.SetupSDKLogs(3, Path.Combine(this.appConfig.DestinationFolder, "SdkLog"), false);
        }

        public bool Login()
        {
            if (this.userId < 0)
            {
                DeviceInfo deviceInfo = null;
                this.userId = this.sdk.Login(this.appConfig.IpAddress, this.appConfig.PortNumber, this.appConfig.UserName, this.appConfig.Password, ref deviceInfo);

                C.ColorWriteLine("Login Success!", ConsoleColor.DarkGreen, DateTime.Now);

                this.channel = deviceInfo.StartChannel;

                return true;
            }

            C.WriteLine("Already logged in", ConsoleColor.Red);
            return false;
        }

        public bool StartDownloadByName(FindResult file)
        {
            if (this.IsDownloading)
            {
                C.WriteLine("Downloading, please stop firstly!");
                return false;
            }

            string directory = this.GetWorkingDirectory(file);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            this.PrintFileInfo(file);
            string fileName = this.GetFullPath(file, directory);

            if (File.Exists(fileName))
            {
                C.WriteLine($"- exist ", ConsoleColor.DarkYellow);
                return false;
            }

            this.downloadHandle = this.sdk.GetFileByName(this.userId, file.FileName, fileName);
            this.downloadFile = file;
            this.progress = new ProgressBar();
            return true;
        }

        public void StopDownload()
        {
            if (this.IsDownloading)
            {
                this.sdk.StopDownoloadFile(this.downloadHandle);
                this.ResetDownloadStatus();
            }
        }

        public void CheckProgress()
        {
            int barValue = this.sdk.GetDownloadPos(this.downloadHandle);

            if (barValue > ProgressBarMinimum && barValue < ProgressBarMaximum)
            {
                this.progress.Report((double)barValue / 100);
            }
            else if (barValue == 100)
            {
                this.StopDownload();
                this.downloadFile = null;

                C.WriteLine("- downloaded", ConsoleColor.Green);
            }
            else if (barValue == 200)
            {
                C.WriteLine("The downloading is abnormal for the abnormal network!", ConsoleColor.DarkRed);

                this.ForceExit();
            }
        }

        public void Logout()
        {
            if (this.userId >= 0)
            {
                C.WriteLine($"Logout the device", timeStamp: DateTime.Now);
                this.sdk.Logout(this.userId);
                this.userId = -1;
            }
        }

        public void ForceExit()
        {
            this.StopDownload();
            this.DeleteCurrentFile();
            this.Logout();
        }

        public async Task<IList<FindResult>> Find(DateTime periodStart, DateTime periodEnd)
        {
            return await this.sdk.Find(periodStart, periodEnd, this.userId, this.channel);
        }

        private void PrintFileInfo(FindResult file)
        {
            C.Write($"{file.FileName}, {file.StartTime}, {file.StopTime}, {Utils.FormatBytes(file.FileSize)} ");
        }

        private string GetWorkingDirectory(FindResult file)
        {
            return Path.Combine(this.appConfig.DestinationFolder, $"{file.StartTime.Year:0000}-{file.StartTime.Month:00}-{file.StartTime.Day:00}");
        }

        private string GetFullPath(FindResult file, string directory = null)
        {
            string folder = directory ?? this.GetWorkingDirectory(file);
            return Path.Combine(folder, $"{file.StartTime.ToString("hhmmss")}_{file.StopTime.ToString("hhmmss")}_{file.FileName}.mp4");
        }

        private void ResetDownloadStatus()
        {
            this.downloadHandle = -1;
            this.progress.Dispose();
            this.progress = null;
        }

        private void DeleteCurrentFile()
        {
            if (this.downloadFile != null)
            {
                string path = this.GetFullPath(this.downloadFile);
                C.WriteLine($"Removing file {path}", ConsoleColor.DarkRed);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                this.downloadFile = null;
            }
        }
    }
}