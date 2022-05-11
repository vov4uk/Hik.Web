using System;

namespace Hik.DTO.Contracts
{
    public class ExceptionLogDto
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public DateTime Created { get; set; }

        public uint? HikErrorCode { get; set; }

        public string Message { get; set; }

        public string CallStack { get; set; }
    }
}
