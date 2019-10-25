using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HikConsole.Data;
using HikConsole.Helpers;
using HikConsole.SDK;
using Newtonsoft.Json;
using C = HikConsole.Helpers.ConsoleHelper;

namespace HikConsole
{
    public static class Program
    {
        private static HikConsole downloader;
        private static AppConfig appConfig;

        public static void Main()
        {
            appConfig = JsonConvert.DeserializeObject<AppConfig>(System.IO.File.ReadAllText("configuration.json"));

            C.WriteLine(appConfig.ToString(), ConsoleColor.DarkMagenta);
            if (appConfig.Mode == "Recurring")
            {
                using (Timer timer = new Timer(async (o) => await DownloadCallback(), null, 0, appConfig.Interval * 60 * 1000))
                {
                    C.WriteLine("Press \'q\' to quit");
                    while (Console.ReadKey() != new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))
                    {
                        // do nothing
                    }

                    downloader?.ForceExit();
                }
            }
            else if (appConfig.Mode == "Fire-and-forget")
            {
                DownloadCallback().GetAwaiter().GetResult();
                C.WriteLine("Press any key to quit.");
                Console.ReadKey();
            }
            else
            {
                C.WriteLine("Invalid config. Press any key to quit.");
                Console.ReadKey();
            }
        }

        private static async Task DownloadCallback()
        {
            C.PrintLine();
            DateTime start = DateTime.Now;
            C.WriteLine($"Start.", timeStamp: start);
            downloader?.Logout();
            downloader = new HikConsole(appConfig, new SDKWrapper());

            try
            {
                downloader.Init();
                if (downloader.Login())
                {
                    DateTime periodStart = start.AddHours(-1 * appConfig.ProcessingPeriodHours);
                    DateTime periodEnd = start;
                    C.WriteLine($"Get videos from {periodStart} to {periodEnd}", timeStamp: DateTime.Now);

                    IList<FindResult> results = await downloader.Find(periodStart, periodEnd);

                    C.WriteLine($"Searching finished", timeStamp: DateTime.Now);
                    C.WriteLine($"Found {results.Count} files\r\n", timeStamp: DateTime.Now);

                    int i = 1;
                    foreach (var file in results)
                    {
                        await DownloadFile(downloader, file, i++, results.Count);
                    }

                    C.WriteLine();
                    downloader.Logout();
                    downloader = null;
                }
            }
            catch (Exception ex)
            {
                C.WriteLine(ex.ToString(), ConsoleColor.Red);
                downloader?.ForceExit();
            }
            finally
            {
                PrintStatistic(start);
                string duration = (start - DateTime.Now).ToString("h'h 'm'm 's's'");
                C.WriteLine($"End. Duration  : {duration}", timeStamp: DateTime.Now);
                C.PrintLine();
            }
        }

        private static void PrintStatistic(DateTime start)
        {
            var firstFile = Utils.GetOldestFile(appConfig.DestinationFolder);
            var lastFile = Utils.GetNewestFile(appConfig.DestinationFolder);
            DateTime.TryParse(firstFile.Directory.Name, out var firstDate);
            DateTime.TryParse(lastFile.Directory.Name, out var lastDate);
            C.WriteLine($"Next execution at {start.AddMinutes(appConfig.Interval)}", timeStamp: DateTime.Now);
            C.ColorWriteLine($"Directory Size : {Utils.FormatBytes(Utils.DirSize(new DirectoryInfo(appConfig.DestinationFolder)))}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Free space     : {Utils.FormatBytes(Utils.GetTotalFreeSpace(appConfig.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Oldest File    : {firstFile.FullName.TrimStart(appConfig.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Newest File    : {lastFile.FullName.TrimStart(appConfig.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Period         : {(int)(lastDate - firstDate).TotalDays} days", ConsoleColor.Red, DateTime.Now);
        }

        private static async Task DownloadFile(HikConsole downloader, FindResult file, int order, int count)
        {
            C.Write($"{order,2}/{count} : ");
            if (downloader.StartDownloadByName(file))
            {
                do
                {
                    await Task.Delay(5000);
                    downloader.CheckProgress();
                }
                while (downloader.IsDownloading);
            }
        }
    }
}
