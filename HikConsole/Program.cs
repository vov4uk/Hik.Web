using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HikConsole.Data;
using HikConsole.Helpers;
using HikConsole.SDK;
using C = HikConsole.Helpers.ConsoleHelper;

namespace HikConsole
{
    class Program
    {
        static HikConsole _downloader;
        static AppConfig _appConfig;
        static async Task Main()
        {
            _appConfig = JsonConvert.DeserializeObject<AppConfig>(System.IO.File.ReadAllText("configuration.json"));

            C.WriteLine(_appConfig.ToString(), ConsoleColor.DarkMagenta);
            if (_appConfig.Mode == "Recurring")
            {
                using (Timer timer = new Timer(async (o) => await DownloadCallback(), null, 0, _appConfig.Interval * 60 * 1000))
                {
                    C.WriteLine("Press \'q\' to quit");
                    while (Console.Read() != 'q')
                    {
                    }

                    _downloader?.ForceExit();
                }
            }
            else if (_appConfig.Mode == "Fire-and-forget")
            {
                await DownloadCallback();
                C.WriteLine("Press any key to quit.");
                Console.ReadKey();
            }
            else
            {
                C.WriteLine("Invalid config. Press any key to quit.");
                Console.ReadKey();
            }
        }

        private async static Task DownloadCallback()
        {
            DateTime start = DateTime.Now;
            C.WriteLine($"Start.", timeStamp: start);
            if (_downloader != null)
            {
                C.WriteLine("Job in working", ConsoleColor.DarkRed, DateTime.Now);
                _downloader.Logout();
            }
            
            DateTime periodStart = start.AddHours(-1* _appConfig.ProcessingPeriodHours);
            DateTime periodEnd = start;

            _downloader = new HikConsole(_appConfig, new SDKWrapper());

            try
            {
                _downloader.Init();
                if (_downloader.Login())
                {
                    C.WriteLine($"Get videos from {periodStart} to {periodEnd}", timeStamp: DateTime.Now);
                    IList<FindResult> results = await _downloader.Find(periodStart, periodEnd);
                    C.WriteLine($"Searching finished", timeStamp: DateTime.Now);
                    C.WriteLine($"Found {results.Count} files\r\n", timeStamp: DateTime.Now);

                    if (results != null && results.Any())
                    {
                        int i = 1;
                        foreach (var file in results)
                        {
                            C.Write($"{i++}/{results.Count} : ");
                            if (_downloader.StartDownloadByName(file))
                            {
                                do
                                {
                                    await Task.Delay(5000);
                                    _downloader.CheckProgress();

                                } while (_downloader.IsDownloading);
                            }
                        }
                    }


                    DateTime end = DateTime.Now;
                    string duration = (start - end).ToString("h'h 'm'm 's's'");

                    C.WriteLine();
                    _downloader.Logout();
                    _downloader = null;
                    C.WriteLine($"End. Duration : {duration}", timeStamp: end);
                    C.WriteLine($"Next execution at {start.AddMinutes(_appConfig.Interval)}", timeStamp: DateTime.Now);
                    C.ColorWriteLine($"DirSize : {Utils.FormatBytes(Utils.DirSize(new DirectoryInfo(_appConfig.DestinationFolder)))}", ConsoleColor.Red, DateTime.Now);
                    C.ColorWriteLine($"Free space : {Utils.FormatBytes(Utils.GetTotalFreeSpace(_appConfig.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                C.PrintError(ex.Message);
                _downloader?.ForceExit();
            }
        }
    }
}
