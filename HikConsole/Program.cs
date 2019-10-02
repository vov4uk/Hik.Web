using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace HikConsole
{
    class Program
    {
        static HikConsole _downloader;
        static AppConfig _appConfig;
        static void Main(string[] args)
        {
            _appConfig = JsonConvert.DeserializeObject<AppConfig>(System.IO.File.ReadAllText("configuration.json"));
            if (_appConfig.Mode == "Recurring")
            {
                using (Timer timer = new Timer((o) => DownloadCallback(), null, 0, _appConfig.Interval * 60 * 1000))
                {
                    Console.WriteLine("Press \'q\' to quit");
                    while (Console.Read() != 'q')
                    {
                    }
                }
            }
            else if (_appConfig.Mode == "Fire-and-forget")
            {
                DownloadCallback();
                Console.WriteLine("Press any key to quit.");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid config. Press any key to quit.");
                Console.ReadKey();
            }
        }

        private static void DownloadCallback()
        {
            DateTime start = DateTime.Now;
            Console.WriteLine($"{start} : Start Job");
            if (_downloader != null)
            {
                Console.WriteLine("Job in working");
                _downloader.Exit();
            }
            
            DateTime dateTimeStart = start.AddHours(-1* _appConfig.ProcessingPeriodHours);
            DateTime dateTimeEnd = start;

            _downloader = new HikConsole(_appConfig);

            if (_downloader.Login())
            {
                Console.WriteLine($"Get videos from {dateTimeStart} to {dateTimeEnd}");
                List<SDK.NET_DVR_FINDDATA_V30> results = _downloader.Search(dateTimeStart, dateTimeEnd).SkipLast(1).ToList();
                _downloader.PrintTable(results);
                foreach (var file in results)
                {
                    if (_downloader.DownloadName(file))
                    {
                        do
                        {
                            _downloader.CheckProgress(file);
                            Thread.Sleep(5000);

                        } while (_downloader.IsDownloading);
                    }
                }

                DateTime end = DateTime.Now;
                string duration = (start - end).ToString("h'h 'm'm 's's'");
                Console.WriteLine($"{end} : End. Duration : {duration}");
                Console.WriteLine($"Next execution at {start.AddMinutes(_appConfig.Interval)}");
                _downloader.Exit();
                _downloader = null;
            }
        }
    }
}
