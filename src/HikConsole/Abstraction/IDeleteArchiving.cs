using System;
using System.Threading.Tasks;
using HikConsole.Config;
using HikConsole.Scheduler;

namespace HikConsole.Abstraction
{
    public interface IDeleteArchiving
    {
        Task<JobResult> Archive(CameraConfig[] cameras, TimeSpan time);
    }
}
