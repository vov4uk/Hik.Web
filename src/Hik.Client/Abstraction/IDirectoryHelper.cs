using System.Collections.Generic;

namespace Hik.Client.Abstraction
{
    public interface IDirectoryHelper
    {
        long GetTotalFreeSpace(string destination);

        long GetTotalSpace(string destination);

        long DirSize(string path);

        void DeleteEmptyFolders(string destination);

        bool DirectoryExists(string destination);

        List<string> EnumerateFiles(string destination);
    }
}
