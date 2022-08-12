using System.Threading.Tasks;

namespace Hik.Helpers.Abstraction
{
    public interface IVideoHelper
    {
        Task<int> GetDuration(string path);

        Task<string> GetThumbnailStringAsync(string path, int width, int height);
    }
}
