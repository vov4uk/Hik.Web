using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Data;
using HikConsole.Helpers;
using C = HikConsole.Helpers.ConsoleHelper;

namespace HikConsole
{
    public class HikClient
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;
        private const string DateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string TimeFormat = "HHmmss";
        private readonly CameraConfig config;
        private readonly IHikService hikService;
        private readonly IFilesHelper filesHelper;
        private readonly IProgressBarFactory progressFactory;
        private int downloadId = -1;
        private RemoteVideoFile currentDownloadFile;
        private LoginResult loginResult;
        private IProgressBar progress;

        public HikClient(CameraConfig config, IHikService sdk, IFilesHelper filesHelper, IProgressBarFactory progressFactory)
        {
            this.config = config;
            this.hikService = sdk;
            this.filesHelper = filesHelper;
            this.progressFactory = progressFactory;
        }

        public bool IsDownloading => this.downloadId >= 0;

        public void InitializeClient()
        {
            this.hikService.Initialize();
            this.hikService.SetupLogs(3, this.filesHelper.CombinePath(this.config.DestinationFolder, "SdkLog"), false);

            this.filesHelper.FolderCreateIfNotExist(this.config.DestinationFolder);
        }

        public bool Login()
        {
            if (this.loginResult == null)
            {
                this.loginResult = this.hikService.Login(this.config.IpAddress, this.config.PortNumber, this.config.UserName, this.config.Password);
                return true;
            }

            C.WriteLine("Already logged in", ConsoleColor.Red);
            return false;
        }

        public bool StartDownload(RemoteVideoFile file)
        {
            if (!this.IsDownloading)
            {
                string workingDirectory = this.GetWorkingDirectory(file);
                this.filesHelper.FolderCreateIfNotExist(workingDirectory);

                this.PrintFileInfo(file);
                string fileName = this.GetFullPath(file, workingDirectory);

                if (!this.filesHelper.FileExists(fileName, file.Size))
                {
                    this.downloadId = this.hikService.StartDownloadFile(this.loginResult.UserId, file.Name, fileName);
                    this.currentDownloadFile = file;
                    this.progress = this.progressFactory?.Create();
                    return true;
                }

                C.WriteLine($"- exist ", ConsoleColor.DarkYellow);
                return false;
            }

            C.WriteLine("Downloading, please stop firstly!");
            return false;
        }

        public void StopDownload()
        {
            if (this.IsDownloading)
            {
                this.hikService.StopDownloadFile(this.downloadId);
                this.ResetDownloadStatus();
            }
        }

        public void CheckProgress()
        {
            if (this.IsDownloading)
            {
                int barValue = this.hikService.GetDownloadPosition(this.downloadId);

                if (barValue > ProgressBarMinimum && barValue < ProgressBarMaximum)
                {
                    this.progress?.Report((double)barValue / 100);
                }
                else if (barValue == 100)
                {
                    this.StopDownload();
                    this.currentDownloadFile = null;

                    C.WriteLine("- downloaded", ConsoleColor.Green);
                }
                else if (barValue == 200)
                {
                    C.WriteLine("The downloading is abnormal for the abnormal network!", ConsoleColor.DarkRed);

                    this.ForceExit();
                }
            }
        }

        public void Logout()
        {
            if (this.loginResult != null)
            {
                C.WriteLine($"Logout the device", timeStamp: DateTime.Now);
                this.hikService.Logout(this.loginResult.UserId);
                this.hikService.Cleanup();
                this.loginResult = null;
            }
        }

        public void ForceExit()
        {
            C.WriteLine("\r\nForce exit", ConsoleColor.DarkRed);
            this.StopDownload();
            this.DeleteCurrentFile();
            this.Logout();
        }

        public async Task<IList<RemoteVideoFile>> Find(DateTime periodStart, DateTime periodEnd)
        {
            this.ValidateDateParameters(periodStart, periodEnd);

            return await this.Find(periodStart, periodEnd, this.loginResult);
        }

        private void PrintFileInfo(RemoteVideoFile file)
        {
            C.Write($"{file.Name}, {file.StartTime.ToString(DateTimePrintFormat)}, {file.StopTime.ToString(DateTimePrintFormat)}, {Utils.FormatBytes(file.Size)} ");
        }

        private string GetWorkingDirectory(RemoteVideoFile file)
        {
            return this.filesHelper.CombinePath(this.config.DestinationFolder, $"{file.StartTime.Year:0000}-{file.StartTime.Month:00}-{file.StartTime.Day:00}");
        }

        private string GetFullPath(RemoteVideoFile file, string directory = null)
        {
            string folder = directory ?? this.GetWorkingDirectory(file);
            return this.filesHelper.CombinePath(folder, $"{file.StartTime.ToString(TimeFormat)}_{file.StopTime.ToString(TimeFormat)}_{file.Name}.mp4");
        }

        private void ResetDownloadStatus()
        {
            this.downloadId = -1;
            this.progress?.Dispose();
            this.progress = null;
        }

        private void DeleteCurrentFile()
        {
            if (this.currentDownloadFile != null)
            {
                string path = this.GetFullPath(this.currentDownloadFile);
                C.WriteLine($"Removing file {path}", ConsoleColor.DarkRed);
                this.filesHelper.DeleteFile(path);

                this.currentDownloadFile = null;
            }
        }

        private void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grather than end");
            }
        }

        private async Task<IList<RemoteVideoFile>> Find(DateTime periodStart, DateTime periodEnd, LoginResult loginResult)
        {
            return await this.hikService.SearchVideoFilesAsync(periodStart, periodEnd, loginResult.UserId, loginResult.Device.StartChannel);
        }
    }
}