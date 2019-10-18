using HikConsole.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using C = HikConsole.Helpers.ConsoleHelper;
using HikConsole.Data;
using HikConsole.Helpers;

namespace HikConsole
{
    public class HikConsole
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;
        
        private int _downloadHandle = -1;
        private FindResult _downloadFile;
        private int _userId = -1;
        private int _channel = -1;
        private ProgressBar _progress;
        private readonly AppConfig _appConfig;
        private readonly ISDKWrapper _sdk;

        public HikConsole(AppConfig appConfig, ISDKWrapper sdk)
        {
            _appConfig = appConfig;
            _sdk = sdk;
        }

        public void Init()
        {
            _sdk.Initialize();
            _sdk.SetupSDKLogs(3, Path.Combine(_appConfig.DestinationFolder, "SdkLog"), false);
        }

        public bool IsDownloading => _downloadHandle >= 0;

        public bool Login()
        {
            if (_userId < 0)
            {
                DeviceInfo _deviceInfo = null;
                _userId = _sdk.Login(_appConfig.IpAddress, _appConfig.PortNumber, _appConfig.UserName, _appConfig.Password, ref _deviceInfo);

                C.ColorWriteLine("Login Success!", ConsoleColor.DarkGreen, DateTime.Now);

                _channel = _deviceInfo.StartChannel;

                return true;
            }
            C.WriteLine("Already logged in", ConsoleColor.Red);
            return false;
        }

        public bool StartDownloadByName(FindResult file)
        {
            if (IsDownloading)
            {
                C.WriteLine("Downloading, please stop firstly!");
                return false;
            }

            string directory = GetWorkingDirectory(file);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            PrintFileInfo(file);
            string fileName = GetFullPath(file, directory);

            if (File.Exists(fileName))
            {
                C.WriteLine($"- exist ", ConsoleColor.DarkYellow);
                return false;
            }

            _downloadHandle =_sdk.GetFileByName(_userId, file.FileName, fileName);
            _downloadFile = file;
            _progress = new ProgressBar();
            return true;
        }

        public void StopDownload()
        {
            if (IsDownloading)
            {
                _sdk.StopDownoloadFile(_downloadHandle);
                ResetDownloadStatus();
            }
        }

        public void CheckProgress()
        {
            int barValue = _sdk.GetDownloadPos(_downloadHandle);

            if (barValue > ProgressBarMinimum && barValue < ProgressBarMaximum)
            {
                _progress.Report((double)barValue / 100);
            }
            else if (barValue == 100)
            {
                StopDownload();
                _downloadFile = null;

                C.WriteLine("- downloaded", ConsoleColor.Green);
            }
            else if (barValue == 200)
            {
                C.WriteLine("The downloading is abnormal for the abnormal network!", ConsoleColor.DarkRed);

                ForceExit();
            }
        }

        public void Logout()
        {
            if (_userId >= 0)
            {
                C.WriteLine($"Logout the device", timeStamp  : DateTime.Now);
                _sdk.Logout(_userId);
                _userId = -1;
            }
        }

        public void ForceExit()
        {
            StopDownload();
            DeleteCurrentFile();
            Logout();
        }

        public async Task<IList<FindResult>> Find(DateTime periodStart, DateTime periodEnd)
        {
            return await _sdk.Find(periodStart, periodEnd, _userId, _channel);
        }

        private void PrintFileInfo(FindResult file)
        {
            C.Write($"{file.FileName}, {file.StartTime}, {file.StopTime}, {Utils.FormatBytes(file.FileSize)} ");
        }

        private string GetWorkingDirectory(FindResult file)
        {
            return Path.Combine(_appConfig.DestinationFolder, $"{file.StartTime.Year:0000}-{file.StartTime.Month:00}-{file.StartTime.Day:00}");
        }
        private string GetFullPath(FindResult file, string directory = null)
        {
            string folder = directory ?? GetWorkingDirectory(file);
            return Path.Combine(folder, $"{file.StartTime.ToString("hhmmss")}_{file.StopTime.ToString("hhmmss")}_{file.FileName}.mp4");
        }

        private void ResetDownloadStatus()
        {
            _downloadHandle = -1;
            _progress.Dispose();
            _progress = null;
        }

        private void DeleteCurrentFile()
        {
            if (_downloadFile != null)
            {
                string path = GetFullPath(_downloadFile);
                C.WriteLine($"Removing file {path}", ConsoleColor.DarkRed);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                _downloadFile = null;
            }
        }
    }
}