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

        private readonly int[] _channelNumbers = new int[96];
        
        private int _downloadHandle = -1;
        private int _userId = -1;
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

                UpdateChanelsInfo(_deviceInfo);

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

        public void CheckProgress(FindResult file)
        {
            int barValue = _sdk.GetDownloadPos(_downloadHandle);

            if (barValue > ProgressBarMinimum && barValue < ProgressBarMaximum)
            {
                _progress.Report((double)barValue / 100);
            }
            else if (barValue == 100)
            {
                StopDownload();

                C.WriteLine("- downloaded", ConsoleColor.Green);
            }
            else if (barValue == 200)
            {
                C.WriteLine("The downloading is abnormal for the abnormal network!", ConsoleColor.DarkRed);

                string path = GetFullPath(file);
                C.WriteLine($"Removing file {path}", ConsoleColor.DarkRed);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                Exit();
            }
        }

        public void Exit()
        {
            StopDownload();

            if (_userId >= 0)
            {
                C.WriteLine($"Logout the device", timeStamp  : DateTime.Now);
                _sdk.Logout(_userId);
                _userId = -1;
            }
        }

        public async Task<IList<FindResult>> Find(DateTime periodStart, DateTime periodEnd)
        {
            return await _sdk.Find(periodStart, periodEnd, _userId, _channelNumbers[0]);
        }

        private void ListChannels(int iChanNo, byte enable)
        {
            C.ColorWriteLine($"Channel {iChanNo} : {(enable == 0 ? "Disabled" : "Enabled")}", ConsoleColor.DarkGreen, DateTime.Now);
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

        private void UpdateChanelsInfo(DeviceInfo device)
        {
            if (device == null)
                throw new ArgumentNullException("Device");

            for (var i = 0; i < device.ChannelNumber; i++)
            {
                ListChannels(i + 1, 1);
                _channelNumbers[i] = i + device.StartChannel;
            }
        }

        private void ResetDownloadStatus()
        {
            _downloadHandle = -1;
            _progress.Dispose();
            _progress = null;
        }
    }
}