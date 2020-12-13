using System;
using HikConsole.DTO.Contracts;

namespace HikConsole.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        public FileDownloadedEventArgs(MediaFileBase file)
        {
            this.File = file;
        }

        public MediaFileBase File { get; }
    }
}
