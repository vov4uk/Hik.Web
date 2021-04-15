using System;

namespace Hik.Client.Abstraction
{
    public interface IFilesHelper
    {
        string CombinePath(params string[] args);

        bool FileExists(string path, long size);

        bool FileExists(string path);

        public long FileSize(string path);

        void DeleteFile(string path);

        void RenameFile(string oldFileName, string newFileName);

        string ReadAllText(string path);

        DateTime GetCreationDate(string path);

        string GetFileNameWithoutExtension(string path);

        string GetFileName(string path);

        string GetExtension(string path);

        string GetTempFileName();
    }
}
