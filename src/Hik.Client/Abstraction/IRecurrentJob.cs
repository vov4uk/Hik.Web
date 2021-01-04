using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Events;
using Hik.DTO.Config;

namespace Hik.Client.Abstraction
{
    public interface IRecurrentJob<T>
    {
        event EventHandler<ExceptionEventArgs> ExceptionFired;

        Task<IReadOnlyCollection<T>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to);
    }
}
