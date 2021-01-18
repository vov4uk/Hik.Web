using System;

namespace Hik.DTO.Contracts
{
    public class FileDTO : MediaFileBase
    {
        public DateTime Date { get; set; }

        public int Duration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
