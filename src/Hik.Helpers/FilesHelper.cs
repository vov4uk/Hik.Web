using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Hik.Helpers.Abstraction;

namespace Hik.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FilesHelper : IFilesHelper
    {
        private const string DateFormat = "yyyyMMdd";

        public string CombinePath(params string[] args)
        {
            return Path.Combine(args);
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

            return 0;
        }

        public void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException)
            {
                // ОК
            }
        }

        public Task<string> ReadAllText(string path)
        {
            return File.ReadAllTextAsync(path);
        }

        public Task<byte[]> ReadAllBytesAsync(string path)
        {
            return File.ReadAllBytesAsync(path);
        }

        public async Task<MemoryStream> ReadAsMemoryStreamAsync(string path)
        {
            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return memory;
        }

        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
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
                date = new FileInfo(path).CreationTime;
            }

            return date;
        }

        public string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public string GetDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(path);
        }

        public string CompressFile(string path)
        {
            string newPath = Path.ChangeExtension(path, ".zip");
            using (ZipArchive archive = ZipFile.Open(newPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(path, Path.GetFileName(path), CompressionLevel.Optimal);
            }

            return newPath;
        }
    }
}
