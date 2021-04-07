using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Hik.Client.Abstraction;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FilesHelper : IFilesHelper
    {
        private const string DateFormat = "yyyyMMdd";

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
            return this.FileSize(path) == size;
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public long FileSize(string path)
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return info.Length;
            }

            return -1;
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

        public void RenameFile(string oldFileName, string newFileName)
        {
            if (!File.Exists(newFileName))
            {
                File.Move(oldFileName, newFileName);
            }
            else
            {
                File.Delete(oldFileName);
            }
        }

        public DateTime GetCreationDate(string path)
        {
            var fileName = Path.GetFileName(path);
            if (!DateTime.TryParseExact(fileName.Substring(0, 8), DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
            {
                var fileInfo = new FileInfo(path);
                date = fileInfo.CreationTime;
            }

            return date;
        }

        public string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public string GetTempFileName()
        {
            return Path.GetTempFileName();
        }
    }
}
