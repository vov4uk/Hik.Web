using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Hik.Quartz.Services
{
    public interface ICronService
    {
        Task RestartSchedulerAsync(IConfiguration configuration);
    }
}
