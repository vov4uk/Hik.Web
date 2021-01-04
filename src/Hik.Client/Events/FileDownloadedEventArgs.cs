using System;
using Hik.DTO.Contracts;

namespace Hik.Client.Events
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
