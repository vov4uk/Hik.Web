namespace Hik.Api.Abstraction
{
    public interface IRemoteFile
    {
        string Name { get; }

        long Size { get; }

        string ToUserFriendlyString();

        string ToDirectoryNameString();

        string ToFileNameString();
    }
}
