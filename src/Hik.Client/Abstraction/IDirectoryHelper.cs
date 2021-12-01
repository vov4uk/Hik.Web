using System.Collections.Generic;

namespace Hik.Client.Abstraction
{
    public interface IDirectoryHelper
    {
        void CreateDirIfNotExist(string path);

        double GetTotalFreeSpaceGb(string path);

        double GetTotalSpaceGb(string path);

        long DirSize(string path);

        void DeleteEmptyDirs(string path);

        bool DirExist(string path);

        List<string> EnumerateFiles(string path, string[] extentions);

        List<string> EnumerateAllDirectories(string path);
    }
}
