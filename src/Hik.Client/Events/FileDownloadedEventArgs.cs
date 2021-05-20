using System;
using Hik.DTO.Contracts;

namespace Hik.Client.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        public FileDownloadedEventArgs(MediaFileDTO file)
        {
            this.File = file;
        }

        public MediaFileDTO File { get; }
    }
}
