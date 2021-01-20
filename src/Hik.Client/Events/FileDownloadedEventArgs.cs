using System;
using Hik.DTO.Contracts;

namespace Hik.Client.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        public FileDownloadedEventArgs(FileDTO file)
        {
            this.File = file;
        }

        public FileDTO File { get; }
    }
}
