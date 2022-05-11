using System;

namespace Hik.DTO.Contracts
{
    public class HikJobDto
    {
        public int Id { get; set; }

        public string JobTrigger { get; set; }

        public bool Success { get; set; } = true;

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public DateTime Started { get; set; }

        public DateTime? Finished { get; set; }

        public int FilesCount { get; set; }

        public ExceptionLogDto Error { get; set; }
    }
}
