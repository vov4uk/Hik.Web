using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Abstraction
{
    public interface IRecurrentJob
    {
        event EventHandler<ExceptionEventArgs> ExceptionFired;

        Task<IReadOnlyCollection<MediaFileDTO>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to);
    }
}
