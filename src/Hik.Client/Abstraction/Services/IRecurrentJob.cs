using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Abstraction.Services
{
    public interface IRecurrentJob
    {
        Task<Result<IReadOnlyCollection<MediaFileDto>>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to);
    }
}
