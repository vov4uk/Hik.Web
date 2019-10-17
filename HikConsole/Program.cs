using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using C = HikConsole.ConsoleHelper;

namespace HikConsole
{
    class Program
    {
        static HikConsole _downloader;
        static AppConfig _appConfig;
        static void Main()
        {
            _appConfig = JsonConvert.DeserializeObject<AppConfig>(System.IO.File.ReadAllText("configuration.json"));
            C.WriteLine(_appConfig.ToString(), ConsoleColor.DarkMagenta);
            if (_appConfig.Mode == "Recurring")
            {
                using (Timer timer = new Timer((o) => DownloadCallback(), null, 0, _appConfig.Interval * 60 * 1000))
                {
                    C.WriteLine("Press \'q\' to quit");
                    while (Console.Read() != 'q')
                    {
                    }
                }
            }
            else if (_appConfig.Mode == "Fire-and-forget")
            {
                DownloadCallback();
                C.WriteLine("Press any key to quit.");
                Console.ReadKey();
            }
            else
            {
                C.WriteLine("Invalid config. Press any key to quit.");
                Console.ReadKey();
            }
        }

        private static void DownloadCallback()
        {
            DateTime start = DateTime.Now;
            C.WriteLine($"Start.", timeStamp: start);
            if (_downloader != null)
            {
                C.WriteLine("Job in working", timeStamp: DateTime.Now);
                _downloader.Exit();
            }
            
            DateTime periodStart = start.AddHours(-1* _appConfig.ProcessingPeriodHours);
            DateTime periodEnd = start;

            _downloader = new HikConsole(_appConfig, new SDKWrapper());
            _downloader.Init();
            if (_downloader.Login())
            {
                C.WriteLine($"Get videos from {periodStart} to {periodEnd}", timeStamp: DateTime.Now);
                List<SDK.NET_DVR_FINDDATA_V30> results = _downloader.Search(periodStart, periodEnd)?.SkipLast(1).ToList();
                C.WriteLine($"Found {results.Count} files\r\n", timeStamp: DateTime.Now);

                if (results != null && results.Any())
                {
                    int i = 1;
                    foreach (var file in results)
                    {
                        C.Write($"{i++}/{results.Count} : ");
                        if (_downloader.DownloadByName(file))
                        {
                            do
                            {
                                Thread.Sleep(5000);
                                _downloader.CheckProgress(file);                                

                            } while (_downloader.IsDownloading);
                        }
                    }
                }

                DateTime end = DateTime.Now;
                string duration = (start - end).ToString("h'h 'm'm 's's'");
                C.WriteLine();
                C.WriteLine($"End. Duration : {duration}", timeStamp: end);
                C.WriteLine($"Next execution at {start.AddMinutes(_appConfig.Interval)}", timeStamp: DateTime.Now);
                _downloader.Exit();
                _downloader = null;
            }
        }
    }
}
