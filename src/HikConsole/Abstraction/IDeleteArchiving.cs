using System;
using HikConsole.Config;
using HikConsole.Scheduler;

namespace HikConsole.Abstraction
{
    public interface IDeleteArchiving
    {
        JobResult Archive(CameraConfig[] cameras, TimeSpan time);
    }
}
