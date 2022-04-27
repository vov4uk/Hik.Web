using System;
using Hik.Client.Events;

namespace Hik.Client.Abstraction
{
    public interface IHikVideoDownloaderService : IRecurrentJob
    {
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
    }
}
