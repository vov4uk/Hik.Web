using HikConsole.DTO.Contracts;
using System.Collections.Generic;

namespace HikConsole.DTO
{
    public class CameraResult
    {
        public CameraResult(CameraDTO config)
        {
            this.Config = config;
            this.DownloadedPhotos = new List<PhotoDTO>();
            this.DownloadedVideos = new List<VideoDTO>();
            this.DeletedFiles = new List<DeletedFileDTO>();
        }

        public CameraDTO Config { get; }

        public bool Failed { get; set; }

        public List<VideoDTO> DownloadedVideos { get; }

        public List<PhotoDTO> DownloadedPhotos { get; }

        public List<DeletedFileDTO> DeletedFiles { get; }

        public HardDriveStatusDTO HardDriveStatus { get; set; }
    }
}
