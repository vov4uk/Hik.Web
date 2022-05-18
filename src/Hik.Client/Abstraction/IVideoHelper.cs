using System.Threading.Tasks;

namespace Hik.Client.Abstraction
{
    public interface IVideoHelper
    {
        Task<int> GetDuration(string path);

        Task<string> GetThumbnailStringAsync(string path);
    }
}
