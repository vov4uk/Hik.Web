using System;
using Hik.Client.Events;

namespace Hik.Client.Abstraction.Services
{
    public interface IHikVideoDownloaderService : IRecurrentJob
    {
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
    }
}
