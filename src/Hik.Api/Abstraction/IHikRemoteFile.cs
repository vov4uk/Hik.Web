namespace Hik.Api.Abstraction
{
    public interface IHikRemoteFile
    {
        string Name { get; }

        long Size { get; }

        string ToUserFriendlyString();

        string ToDirectoryNameString();

        string ToYiDirectoryNameString();

        string ToFileNameString();

        string ToYiFileNameString();
    }
}
