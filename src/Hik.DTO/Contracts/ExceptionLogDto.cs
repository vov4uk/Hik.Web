using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class ExceptionLogDto
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        [Display(Name = "Taken"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Created { get; set; }

        public uint? HikErrorCode { get; set; }

        public string Message { get; set; }

        public string CallStack { get; set; }
    }
}
