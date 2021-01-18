using Hik.DTO.Contracts;
using System.Collections.Generic;

namespace Hik.DTO
{
    public class CameraResult
    {
        public CameraResult(CameraDTO config)
        {
            Config = config;
            DownloadedPhotos = new List<PhotoDTO>();
            DownloadedVideos = new List<VideoDTO>();
            DeletedFiles = new List<DeletedFileDTO>();
        }

        public CameraDTO Config { get; }

        public List<VideoDTO> DownloadedVideos { get; }

        public List<PhotoDTO> DownloadedPhotos { get; }

        public List<DeletedFileDTO> DeletedFiles { get; }
    }
}
