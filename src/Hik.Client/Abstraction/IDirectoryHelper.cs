using System.Collections.Generic;

namespace Hik.Client.Abstraction
{
    public interface IDirectoryHelper
    {
        void CreateDirIfNotExist(string path);

        long GetTotalFreeSpace(string path);

        long GetTotalSpace(string path);

        long DirSize(string path);

        void DeleteEmptyDirs(string path);

        bool DirExist(string path);

        List<string> EnumerateFiles(string path);

        List<string> EnumerateAllDirectories(string path);

        List<string> EnumerateTopDirectories(string path);
    }
}
