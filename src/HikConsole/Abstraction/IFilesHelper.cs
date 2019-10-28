namespace HikConsole.Abstraction
{
    public interface IFilesHelper
    {
        string CombinePath(params string[] args);

        void FolderCreateIfNotExist(string path);

        bool FileExists(string path, long size);

        void DeleteFile(string path);

        string ReadAllText(string path);
    }
}
