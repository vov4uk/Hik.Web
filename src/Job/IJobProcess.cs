using System.Threading.Tasks;
using Hik.DataAccess.Data;

namespace Job
{
    public interface IJobProcess
    {
        Task ExecuteAsync();

        HikJob JobInstance { get; }
    }
}
