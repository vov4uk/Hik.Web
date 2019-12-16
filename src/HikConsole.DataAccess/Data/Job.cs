using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("Job")]
    public class Job
    {
        [Key]
        public int Id { get; set; }

        public int FailsCount { get; set; }

        public  DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public DateTime Started { get; set; }

        public DateTime Finished { get; set; }

    }
}
