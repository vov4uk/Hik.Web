using System.Threading.Tasks;

namespace Job
{
    public interface IJobProcess
    {
        Task ExecuteAsync();
    }
}
