using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleTables;

namespace HikConsole
{
    public class HikConsole
    {
        private const int PROGRESS_BAR_MAXIMUM = 100;
        private const int PROGRESS_BAR_MINIMUM = 0;
        private SDK.NET_DVR_DEVICEINFO_V30 _deviceInfo;
        private uint _dwAChanTotalNum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96, ArraySubType = UnmanagedType.U4)]
        private readonly int[] _iChannelNum = new int[96];
        
        private int _downloadHandle = -1;
        private int _findHandle = -1;
        private int _userId = -1;
        private ProgressBar _progress;
        private int _progressBarValue;
        private readonly AppConfig _appConfig;

        public HikConsole(AppConfig appConfig)
        {
            this._appConfig = appConfig;
            Init();
        }

        public void Init()
        {
            if (!SDK.NET_DVR_Init())
            {
                PrintError("NET_DVR_Init");
                return;
            }
            SDK.NET_DVR_SetLogToFile(3, Path.Combine(_appConfig.DestinationFolder, "SdkLog"), true);
        }

        public bool IsDownloading => _downloadHandle >= 0;

        public bool Login()
        {
            if (_userId < 0)
            {
                _userId = SDK.NET_DVR_Login_V30(_appConfig.IpAddress, _appConfig.PortNumber, _appConfig.UserName, _appConfig.Password, ref _deviceInfo);
                if (_userId < 0)
                {
                    PrintError("NET_DVR_Login_V30", "Unable to login, check configuration.json for correct credentials");
                    return false;
                }
                else
                {
                    WriteLine("Login Success!", ConsoleColor.DarkGreen);

                    _dwAChanTotalNum = _deviceInfo.byChanNum;
                    for (var i = 0; i < _dwAChanTotalNum; i++)
                    {
                        ListAnalogChannel(i + 1, 1);
                        _iChannelNum[i] = i + _deviceInfo.byStartChan;
                    }

                    return true;
                }
            }
            WriteLine("Already logged in", ConsoleColor.Red);
            return false;
        }

        public bool DownloadName(SDK.NET_DVR_FINDDATA_V30 file)
        {
            if (IsDownloading)
            {
                WriteLine("Downloading, please stop firstly!");
                return false;
            }

            string directory = GetWorkingDirectory(file);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                WriteLine($"Created {directory}", ConsoleColor.DarkYellow);
            }

            string fileName = GetFullPath(file, directory);
            if (File.Exists(fileName))
            {
                WriteLine($"Skip {fileName} ", ConsoleColor.DarkYellow);
                return false;
            }
            WriteLine($"Started to download {fileName} ");
            _downloadHandle = SDK.NET_DVR_GetFileByName(_userId, file.sFileName, fileName);
            if (_downloadHandle < 0)
            {
                PrintError("NET_DVR_GetFileByName");
                return false;
            }

            uint iOutValue = 0;

            if (!SDK.NET_DVR_PlayBackControl_V40(_downloadHandle, SDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0,IntPtr.Zero, ref iOutValue))
            {
                PrintError("NET_DVR_PlayBackControl_V40");
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
                PrintError("NET_DVR_StopGetFile", "Download controlling failed,print error code");
                return;
            }

            WriteLine("The downloading has been stopped successfully!");
            _downloadHandle = -1;
            _progressBarValue = 0;
            _progress.Dispose();
            _progress = null;
        }

        public void CheckProgress(SDK.NET_DVR_FINDDATA_V30 file)
        {
            int barValue = SDK.NET_DVR_GetDownloadPos(_downloadHandle);

            if (barValue > PROGRESS_BAR_MINIMUM && barValue < PROGRESS_BAR_MAXIMUM)
            {
                _progressBarValue = barValue;
                _progress.Report((double) _progressBarValue / 100);
            }
            else if (barValue == 100)
            {
                _progressBarValue = barValue;
                if (!SDK.NET_DVR_StopGetFile(_downloadHandle))
                {
                    PrintError("NET_DVR_StopGetFile", "Download controlling failed,print error code");
                    return;
                }

                _progress.Dispose();
                _progress = null;
                _downloadHandle = -1;
                WriteLine("Downloaded", ConsoleColor.Green);
            }
            else if (barValue == 200)
            {
                WriteLine("The downloading is abnormal for the abnormal network!", ConsoleColor.DarkRed);

                string path = GetFullPath(file);
                WriteLine($"Removing file {path}", ConsoleColor.DarkRed);
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
                WriteLine("Logout the device");
                SDK.NET_DVR_Logout(_userId);
                _userId = -1;
            }
        }

        public IEnumerable<SDK.NET_DVR_FINDDATA_V30> Search(DateTime dateTimeStart, DateTime dateTimeEnd)
        {
            List<SDK.NET_DVR_FINDDATA_V30> results = new List<SDK.NET_DVR_FINDDATA_V30>();

            SDK.NET_DVR_FILECOND_V40 findCond = new SDK.NET_DVR_FILECOND_V40
            {
                lChannel = _iChannelNum[0],
                dwFileType = 0xff,
                dwIsLocked = 0xff,
                struStartTime = new SDK.NET_DVR_TIME(dateTimeStart),
                struStopTime = new SDK.NET_DVR_TIME(dateTimeEnd)
            };

            _findHandle = SDK.NET_DVR_FindFile_V40(_userId, ref findCond);

            if (_findHandle < 0)
            {
                PrintError("NET_DVR_FindFile_V40", "find files failed，print error code");
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
                    WriteLine("Searching is finished");
                    break;
                }
                else
                {
                    break;
                }
            }

            return results;
        }

        public void PrintTable(IEnumerable<SDK.NET_DVR_FINDDATA_V30> list)
        {
            ConsoleTable table = new ConsoleTable("FileName", "Start", "Stop");
            foreach (var fileData in list)
            {
                table.AddRow(fileData.sFileName, fileData.struStartTime, fileData.struStopTime);
            }
            table.Write();
        }

        public void ListAnalogChannel(int iChanNo, byte byEnable)
        {
            WriteLine($"Analog Channel {iChanNo} : {(byEnable == 0 ? "Disabled" : "Enabled")}", ConsoleColor.DarkGreen);
        }

        public void ListIpChannel(int iChanNo, byte byOnline, byte byId)
        {
            WriteLine($"IPCamera {iChanNo} : {(byId == 0 ? "X" : byOnline == 0 ? "offline" : "online")}");
        }

        private void PrintError(string method, string msg = "")
        {
            WriteLine($"{method} failed, error code= {SDK.NET_DVR_GetLastError()} : {msg}", ConsoleColor.Red);
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

        private void WriteLine(string str, ConsoleColor foreground = ConsoleColor.White)
        {
            Console.ForegroundColor = foreground;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }
}