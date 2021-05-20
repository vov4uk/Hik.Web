[![Build Status](https://dev.azure.com/khmelovskyi/HikConsole/_apis/build/status/vov4uk.HikConsole?branchName=master)](https://dev.azure.com/khmelovskyi/HikConsole/_build/latest?definitionId=1&branchName=master)

# Hik.Api
* Simple wrapper over Hikvision SDK, with min functionality do download files from camera. Login, fetch files list, download files, and log out.

```
            IHikApi hikApi = new HikApi();
            hikApi.Initialize();
            hikApi.SetupLogs(3, "C:\\sdkLogsPath", false);
            hikApi.SetConnectTime(2000, 1);
            hikApi.SetReconnect(10000, 1);

            var session = hikApi.Login("192.168.1.64", 8000, "admin", "pass");

            //Get photos files for last 2 hours
            var photos = await hikApi.PhotoService.FindFilesAsync(DateTime.Now.AddHours(-2), DateTime.Now, session);
            foreach (var photo in photos)
            {
                hikApi.PhotoService.DownloadFile(session.UserId, photo.Name, photo.Size, "photoFilePath.jpg");
            }

            //Get video files for last 2 hours
            var videos = await hikApi.VideoService.FindFilesAsync(DateTime.Now.AddHours(-2), DateTime.Now, session);

            foreach (var video in videos)
            {
                var downloadId = hikApi.VideoService.StartDownloadFile(session.UserId, video.Name, "videoFilePath.mp4");
                do
                {
                    await Task.Delay(1000);
                    int downloadProgress = hikApi.VideoService.GetDownloadPosition(downloadId);
                    if (downloadProgress == 100)
                    {
                        hikApi.VideoService.StopDownloadFile(downloadId);
                        break;
                    }
                    else if (downloadProgress < 0 || downloadProgress > 100)
                    {
                        throw new InvalidOperationException($"UpdateDownloadProgress failed, progress value = {downloadProgress}");
                    }
                }
                while (true);
            }

            hikApi.Logout(session.UserId);
            hikApi.Cleanup();
```

# Hik.Client
* Wrapper over Hik.Api. 
* HikPhotoClient - Download photo files from Hikvision IP cameras. 
* HikVideoClient - Download video files from Hikvision IP cameras. 
* YiClient - Download files from Xiaomi YI cameras. Connetct YI camera over FTP, and download files.

# HikWeb
* Download photo/video files from Hikvision IP cameras.
* Download files from FTP
* Migrate files from one folder to another. Move and rename.
* Remove old files if low of space.
* Store job results to SQLite DB.
* Running as Windows Service
* Simple Web UI to view status and statistics.
* Job execution based on cron string.

![Alt text](HikConsole.png?raw=true "Output example")
