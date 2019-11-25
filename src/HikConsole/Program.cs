using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HikConsole.Config;
using HikConsole.Data;
using HikConsole.Helpers;
using HikConsole.Services;
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
                await ProcessCamera(camera, periodStart, periodEnd, appConfig.ShowProgress);
                C.PrintLine(40);
            }

            C.WriteLine($"Next execution at {start.AddMinutes(appConfig.Interval).ToString()}", timeStamp: DateTime.Now);
            string duration = (start - DateTime.Now).ToString("h'h 'm'm 's's'");
            C.WriteLine($"End. Duration  : {duration}", timeStamp: DateTime.Now);
        }

        private static async Task ProcessCamera(CameraConfig camera, DateTime periodStart, DateTime periodEnd, bool showProgress)
        {
            downloader?.Logout();
            downloader = new HikClient(camera, new HikService(), new FilesHelper(), showProgress ? new ProgressBarFactory() : default(ProgressBarFactory));

            try
            {
                downloader.InitializeClient();
                if (downloader.Login())
                {
                    C.ColorWriteLine($"Login success!", ConsoleColor.DarkGreen, DateTime.Now);
                    C.WriteLine(camera.ToString(), ConsoleColor.DarkMagenta);
                    C.WriteLine($"Get videos from {periodStart.ToString()} to {periodEnd.ToString()}", timeStamp: DateTime.Now);

                    List<RemoteVideoFile> results = (await downloader.Find(periodStart, periodEnd)).ToList();

                    C.WriteLine($"Searching finished", timeStamp: DateTime.Now);
                    C.WriteLine($"Found {results.Count.ToString()} files\r\n", timeStamp: DateTime.Now);

                    int i = 1;
                    foreach (var file in results)
                    {
                        await DownloadFile(downloader, file, i++, results.Count);
                    }

                    C.WriteLine();
                    downloader.Logout();
                    downloader = null;
                }

                PrintStatistic(camera);
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
                C.WriteLine(msg, ConsoleColor.Red);

                EmailHelper.SendEmail(appConfig.EmailConfig, msg);
                downloader?.ForceExit();
            }
        }

        private static void PrintStatistic(CameraConfig camera)
        {
            System.IO.FileInfo firstFile = Utils.GetOldestFile(camera.DestinationFolder);
            System.IO.FileInfo lastFile = Utils.GetNewestFile(camera.DestinationFolder);
            if (!string.IsNullOrEmpty(firstFile?.FullName) && !string.IsNullOrEmpty(lastFile?.FullName))
            {
                DateTime.TryParse(firstFile.Directory.Name, out var firstDate);
                DateTime.TryParse(lastFile.Directory.Name, out var lastDate);
                TimeSpan period = lastDate - firstDate;
                C.ColorWriteLine($"Directory Size : {Utils.FormatBytes(Utils.DirSize(camera.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
                C.ColorWriteLine($"Free space     : {Utils.FormatBytes(Utils.GetTotalFreeSpace(camera.DestinationFolder))}", ConsoleColor.Red, DateTime.Now);
                C.ColorWriteLine($"Oldest File    : {firstFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
                C.ColorWriteLine($"Newest File    : {lastFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}", ConsoleColor.Red, DateTime.Now);
                C.ColorWriteLine($"Period         : {Math.Floor(period.TotalDays).ToString()} days", ConsoleColor.Red, DateTime.Now);
            }
        }

        private static async Task DownloadFile(HikClient downloader, RemoteVideoFile file, int order, int count)
        {
            C.Write($"{order.ToString(),2}/{count.ToString()} : ");
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
