using Hik.Helpers.Abstraction;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Hik.Helpers
{
    [ExcludeFromCodeCoverage]
    public class DirectoryHelper : IDirectoryHelper
    {
        private const string AllFilter = "*";

        public void CreateDirIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public long DirSize(string path)
        {
            var d = new DirectoryInfo(path);
            long size = 0;

            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }

            // Add sub directory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += this.DirSize(di.FullName);
            }

            return size;
        }

        public long GetTotalFreeSpaceBytes(string path)
        {
            DriveInfo drive = GetDrive(path);

            return drive?.TotalFreeSpace ?? 0;
        }

        public long GetTotalSpaceBytes(string path)
        {
            DriveInfo drive = GetDrive(path);

            return drive?.TotalSize ?? -1;
        }

        public void DeleteEmptyDirs(string path)
        {
            var directories = Directory.EnumerateDirectories(path, AllFilter, SearchOption.AllDirectories);
            foreach (var directory in directories)
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        public bool DirExist(string path)
        {
            return Directory.Exists(path);
        }

        public List<string> EnumerateFiles(string path, string[] extentions)
        {
            return Directory.EnumerateFiles(path, AllFilter, SearchOption.AllDirectories)
                    .Where(s => extentions.Any(ext => ext == Path.GetExtension(s)))
                    .ToList();
        }

        public List<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path, AllFilter, SearchOption.AllDirectories)
                .ToList();
        }

        public List<string> EnumerateAllDirectories(string path)
        {
            return Directory.EnumerateDirectories(path, AllFilter, SearchOption.AllDirectories).OrderBy(x => x).ToList();
        }

        private static DriveInfo GetDrive(string path)
        {
            var driveName = Path.GetPathRoot(path);

            return DriveInfo.GetDrives()
                .FirstOrDefault(x => x.IsReady && x.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
