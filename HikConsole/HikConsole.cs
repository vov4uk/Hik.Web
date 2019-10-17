using HikConsole.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using C = HikConsole.ConsoleHelper;

namespace HikConsole
{
    public class HikConsole
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;

        private uint _channelsTotalNumber;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96, ArraySubType = UnmanagedType.U4)]
        private readonly int[] _channelNumbers = new int[96];
        
        private int _downloadHandle = -1;
        private int _findHandle = -1;
        private int _userId = -1;
        private ProgressBar _progress;
        private int _progressBarValue;
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

                C.Write(timeStamp: DateTime.Now);
                C.WriteLine("Login Success!", ConsoleColor.DarkGreen);

                UpdateChanelsInfo(_deviceInfo);

                return true;
            }
            C.WriteLine("Already logged in", ConsoleColor.Red);
            return false;
        }

        public bool DownloadByName(SDK.NET_DVR_FINDDATA_V30 file)
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

            string fileName = GetFullPath(file, directory);
            if (File.Exists(fileName))
            {
                C.Write($"{file.sFileName}, {file.struStartTime}, {file.struStopTime} ");
                C.WriteLine($"- exist ", ConsoleColor.DarkYellow);
                return false;
            }
            C.Write($"{file.sFileName}, {file.struStartTime}, {file.struStopTime} ");

            _downloadHandle = SDK.NET_DVR_GetFileByName(_userId, file.sFileName, fileName);
            if (_downloadHandle < 0)
            {
                C.PrintError("NET_DVR_GetFileByName");
                return false;
            }

            uint iOutValue = 0;
            if (!SDK.NET_DVR_PlayBackControl_V40(_downloadHandle, SDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                C.PrintError("NET_DVR_PlayBackControl_V40");
                return false;
            }

            _progress = new ProgressBar();
            return true;
        }

        public void StopDownload()
        {
            if (!IsDownloading) return;

            if (!SDK.NET_DVR_StopGetFile(_downloadHandle))
            {
                C.PrintError("NET_DVR_StopGetFile", "Download controlling failed");
                return;
            }

            C.WriteLine($"The downloading has been stopped successfully!", timeStamp : DateTime.Now);
            _downloadHandle = -1;
            _progressBarValue = 0;
            _progress.Dispose();
            _progress = null;
        }

        public void CheckProgress(SDK.NET_DVR_FINDDATA_V30 file)
        {
            int barValue = SDK.NET_DVR_GetDownloadPos(_downloadHandle);

            if (barValue > ProgressBarMinimum && barValue < ProgressBarMaximum)
            {
                _progressBarValue = barValue;
                _progress.Report((double) _progressBarValue / 100);
            }
            else if (barValue == 100)
            {
                _progressBarValue = barValue;
                if (!SDK.NET_DVR_StopGetFile(_downloadHandle))
                {
                    C.PrintError("NET_DVR_StopGetFile", "Download controlling failed");
                    return;
                }

                _progress.Dispose();
                _progress = null;
                _downloadHandle = -1;
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
                SDK.NET_DVR_Logout(_userId);
                _userId = -1;
            }
        }

        public IEnumerable<SDK.NET_DVR_FINDDATA_V30> Search(DateTime periodStart, DateTime periodEnd)
        {
            List<SDK.NET_DVR_FINDDATA_V30> results = new List<SDK.NET_DVR_FINDDATA_V30>();

            SDK.NET_DVR_FILECOND_V40 findCond = new SDK.NET_DVR_FILECOND_V40
            {
                lChannel = _channelNumbers[0],
                dwFileType = 0xff,
                dwIsLocked = 0xff,
                struStartTime = new SDK.NET_DVR_TIME(periodStart),
                struStopTime = new SDK.NET_DVR_TIME(periodEnd)
            };

            _findHandle = SDK.NET_DVR_FindFile_V40(_userId, ref findCond);

            if (_findHandle < 0)
            {
                C.PrintError("NET_DVR_FindFile_V40", "find files failed");
                return null;
            }
            
            while (true)
            {
                SDK.NET_DVR_FINDDATA_V30 fileData = new SDK.NET_DVR_FINDDATA_V30();
                int findResult = SDK.NET_DVR_FindNextFile_V30(_findHandle, ref fileData);

                if (findResult == SDK.NET_DVR_ISFINDING)
                {
                    Thread.Sleep(1000);
                }
                else if (findResult == SDK.NET_DVR_FILE_SUCCESS)
                {
                    results.Add(fileData);
                   
                }
                else if (findResult == SDK.NET_DVR_FILE_NOFIND || findResult == SDK.NET_DVR_NOMOREFILE)
                {
                    C.WriteLine($"Searching is finished", timeStamp : DateTime.Now);
                    break;
                }
                else
                {
                    break;
                }
            }
            return results;
        }

        public void ListAnalogChannel(int iChanNo, byte enable)
        {
            C.Write(timeStamp: DateTime.Now);
            C.WriteLine($"Analog Channel {iChanNo} : {(enable == 0 ? "Disabled" : "Enabled")}", ConsoleColor.DarkGreen);
        }

        public void ListIpChannel(int iChanNo, byte online, byte id)
        {
            C.Write(timeStamp: DateTime.Now);
            C.WriteLine($"IPCamera {iChanNo} : {(id == 0 ? "X" : online == 0 ? "offline" : "online")}");
        }

        private string GetWorkingDirectory(SDK.NET_DVR_FINDDATA_V30 file)
        {
            return Path.Combine(_appConfig.DestinationFolder, $"{file.struStartTime.dwYear:0000}-{file.struStartTime.dwMonth:00}-{file.struStartTime.dwDay:00}");
        }
        private string GetFullPath(SDK.NET_DVR_FINDDATA_V30 file, string directory = null)
        {
            string folder = directory ?? GetWorkingDirectory(file);
            return Path.Combine(folder, $"{file.struStartTime.ToShortString()}_{file.struStopTime.ToShortString()}_{file.sFileName}.mp4");
        }

        private void UpdateChanelsInfo(DeviceInfo device)
        {
            if (device == null)
                throw new ArgumentNullException("Device");

            _channelsTotalNumber = device.ChannelNumber;
            for (var i = 0; i < _channelsTotalNumber; i++)
            {
                ListAnalogChannel(i + 1, 1);
                _channelNumbers[i] = i + device.StartChannel;
            }
        }
    }
}