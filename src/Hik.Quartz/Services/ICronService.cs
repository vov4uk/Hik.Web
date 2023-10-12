using Hik.Quartz.Contracts;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.Quartz.Services
{
    public interface ICronService
    {
        Task<CronDto> GetCronAsync(IConfiguration configuration, string name, string group);

        Task UpdateTriggerAsync(IConfiguration configuration, CronDto cron);

        Task DeleteTriggerAsync(IConfiguration configuration, string group, string name, string className);

        Task<IReadOnlyCollection<CronDto>> GetAllCronsAsync();

        Task RestartSchedulerAsync(IConfiguration configuration);
    }
}
