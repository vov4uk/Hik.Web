using System;
using Hik.DTO.Contracts;

namespace Hik.Client.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        public FileDownloadedEventArgs(MediaFileDto file)
        {
            this.File = file;
        }

        public MediaFileDto File { get; }
    }
}
