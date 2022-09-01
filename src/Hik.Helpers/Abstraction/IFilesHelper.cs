using System;
using System.IO;
using System.Threading.Tasks;

namespace Hik.Helpers.Abstraction
{
    public interface IFilesHelper
    {
        string CombinePath(params string[] args);

        bool FileExists(string path, long size);

        bool FileExists(string path);

        public long FileSize(string path);

        void DeleteFile(string path);

        void RenameFile(string oldFileName, string newFileName);

        Task<string> ReadAllText(string path);

        Task<byte[]> ReadAllBytesAsync(string path);

        Task<MemoryStream> ReadAsMemoryStreamAsync(string path);

        void WriteAllText(string path, string contents);

        DateTime GetCreationDate(string path);

        string GetFileNameWithoutExtension(string path);

        string GetFileName(string path);

        string GetExtension(string path);

        string GetTempFileName();

        string GetDirectoryName(string path);

        string ZipFile(string path);

        void UnZipFile(string path);
    }
}
