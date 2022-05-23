using Hik.Quartz.Contracts;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.Quartz.Services
{
    public interface ICronService
    {
        Task<CronDTO> GetCronAsync(IConfiguration configuration, string name, string group);

        Task UpdateCronAsync(IConfiguration configuration, CronDTO cron);

        Task<IReadOnlyCollection<CronDTO>> GetAllCronsAsync();

        Task RestartSchedulerAsync(IConfiguration configuration);
    }
}
