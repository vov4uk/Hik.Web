namespace HikConsole.Abstraction
{
    public interface IDirectoryHelper
    {
        long GetTotalFreeSpace(string destenation);

        long DirSize(string path);
    }
}
