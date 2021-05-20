using System;

namespace Hik.DTO.Contracts
{
    public class MediaFileDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public long Size { get; set; }

        public DateTime? DownloadStarted { get; set; }

        public int? DownloadDuration { get; set; }

        public DateTime Date { get; set; }

        public int? Duration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
