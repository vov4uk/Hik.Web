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
        private readonly string[] allowedExtentions = new[] { ".mp4", ".jpg", ".ini" };

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

            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += this.DirSize(di.FullName);
            }

            return size;
        }

        public long GetTotalFreeSpace(string destination)
        {
            DriveInfo drive = GetDrive(destination);

            return drive?.TotalFreeSpace ?? -1;
        }

        public long GetTotalSpace(string destination)
        {
            DriveInfo drive = GetDrive(destination);

            return drive?.TotalSize ?? -1;
        }

        public void DeleteEmptyFolders(string destination)
        {
            var directories = Directory.EnumerateDirectories(destination, AllFilter, SearchOption.AllDirectories);
            foreach (var directory in directories)
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        public bool DirectoryExists(string destination)
        {
            return Directory.Exists(destination);
        }

        public List<string> EnumerateFiles(string destination)
        {
            return Directory.EnumerateFiles(destination, AllFilter, SearchOption.AllDirectories)
                    .Where(s => allowedExtentions.Any(ext => ext == Path.GetExtension(s)))
                    .ToList();
        }

        private static DriveInfo GetDrive(string destination)
        {
            var driveName = Path.GetPathRoot(destination);

            var drive = DriveInfo.GetDrives().Where(x => x.IsReady && x.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return drive;
        }
    }
}
