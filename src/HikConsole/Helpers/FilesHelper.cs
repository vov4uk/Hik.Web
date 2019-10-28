using System.Diagnostics.CodeAnalysis;
using System.IO;
using HikConsole.Abstraction;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FilesHelper : IFilesHelper
    {
        public string CombinePath(params string[] args)
        {
            return Path.Combine(args);
        }

        public void FolderCreateIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public bool FileExists(string path, long size)
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return info.Length == size + 40;
            }

            return false;
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}
