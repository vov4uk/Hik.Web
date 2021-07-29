using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Hik.Client.Abstraction;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public class DirectoryHelper : IDirectoryHelper
    {
        private const string AllFilter = "*";
        private const double Gb = 1024.0 * 1024.0 * 1024.0;
        private readonly string[] allowedExtensions = { ".mp4", ".jpg", ".ini" };

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

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

        public double GetTotalFreeSpaceGb(string path)
        {
            DriveInfo drive = GetDrive(path);

            return drive?.TotalFreeSpace / Gb ?? -1.0;
        }

        public double GetTotalSpaceGb(string path)
        {
            DriveInfo drive = GetDrive(path);

            return drive?.TotalSize / Gb ?? -1.0;
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

        public List<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path, AllFilter, SearchOption.AllDirectories)
                    .Where(s => allowedExtensions.Any(ext => ext == Path.GetExtension(s)))
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
