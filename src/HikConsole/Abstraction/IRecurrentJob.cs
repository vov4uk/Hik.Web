using System;
using System.Threading.Tasks;
using HikConsole.Config;
using HikConsole.DTO;

namespace HikConsole.Abstraction
{
    public interface IRecurrentJob
    {
        Task<JobResult> ExecuteAsync(string configFilePath);
    }
}
