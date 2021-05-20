using Hik.Api.Data;

namespace Hik.Api.Abstraction
{
    internal interface ISourceFile
    {
        HikRemoteFile ToRemoteFile();
    }
}
