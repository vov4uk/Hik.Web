using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.Client.Abstraction
{
    public interface IUploaderClient : IClientBase
    {
        Task UploadFilesAsync(IEnumerable<string> localPaths, string remoteDir);
    }
}
