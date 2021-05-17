using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    public class ExceptionLog
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Job")]
        public int JobId { get; set; }

        [Display(Name = "Taken"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Created { get; set; }

        public uint? HikErrorCode { get; set; }

        public string Message { get; set; }

        public string CallStack { get; set; }

        public virtual HikJob Job { get; set; }
    }
}
