using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikConsole.DTO.Config;
using HikConsole.Events;

namespace HikConsole.Abstraction
{
    public interface IRecurrentJob<T>
    {
        event EventHandler<ExceptionEventArgs> ExceptionFired;

        Task<IReadOnlyCollection<T>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to);
    }
}
