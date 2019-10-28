using System;
using System.Collections.Generic;
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
        private static HikClient downloader;
        private static AppConfig appConfig;

        public static void Main()
        {
            appConfig = JsonConvert.DeserializeObject<AppConfig>(new FilesHelper().ReadAllText("configuration.json"));

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
            DateTime start = DateTime.Now;
            C.PrintLine();
            C.WriteLine($"Start.", timeStamp: start);
            DateTime periodStart = start.AddHours(-1 * appConfig.ProcessingPeriodHours);
            DateTime periodEnd = start;

            foreach (var camera in appConfig.Cameras)
            {
                await ProcessCamera(camera, periodStart, periodEnd);
                C.PrintLine(40);
            }

            C.WriteLine($"Next execution at {start.AddMinutes(appConfig.Interval)}", timeStamp: DateTime.Now);
            string duration = (start - DateTime.Now).ToString("h'h 'm'm 's's'");
            C.WriteLine($"End. Duration  : {duration}", timeStamp: DateTime.Now);
        }

        private static async Task ProcessCamera(CameraConfig camera, DateTime periodStart, DateTime periodEnd)
        {
            downloader?.Logout();
            downloader = new HikClient(camera, new SDKWrapper(), new FilesHelper(), new ProgressBarFactory());

            try
            {
                downloader.Init();
                if (downloader.Login())
                {
                    C.ColorWriteLine($"Login success!", ConsoleColor.DarkGreen, DateTime.Now);
                    C.WriteLine(camera.ToString(), ConsoleColor.DarkMagenta);
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
                PrintStatistic(camera);
            }
        }

        private static void PrintStatistic(CameraConfig camera)
        {
            var firstFile = Utils.GetOldestFile(camera.DestinationFolder);
            var lastFile = Utils.GetNewestFile(camera.DestinationFolder);
            DateTime.TryParse(firstFile.Directory.Name, out var firstDate);
            DateTime.TryParse(lastFile.Directory.Name, out var lastDate);
            C.ColorWriteLine($"Directory Size : {Utils.FormatBytes(Utils.DirSize(camera.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Free space     : {Utils.FormatBytes(Utils.GetTotalFreeSpace(camera.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Oldest File    : {firstFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Newest File    : {lastFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
            C.ColorWriteLine($"Period         : {(int)(lastDate - firstDate).TotalDays} days", ConsoleColor.Red, DateTime.Now);
        }

        private static async Task DownloadFile(HikClient downloader, FindResult file, int order, int count)
        {
            C.Write($"{order,2}/{count} : ");
            if (downloader.StartDownload(file))
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
