using System.Collections.Generic;
using HikConsole.Config;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class CameraResult
    {
        public CameraResult(CameraConfig config)
        {
            this.Config = config;
            this.DownloadedPhotos = new List<Photo>();
            this.DownloadedVideos = new List<Video>();
        }

        public CameraConfig Config { get; }

        public bool Failed { get; set; }

        public List<Video> DownloadedVideos { get; }

        public List<Photo> DownloadedPhotos { get; }

        public HardDriveStatus HardDriveStatus { get; set; }
    }
}
