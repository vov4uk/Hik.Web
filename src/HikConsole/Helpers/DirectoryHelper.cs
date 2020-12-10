using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using HikConsole.Abstraction;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public class DirectoryHelper : IDirectoryHelper
    {
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
            var driveName = Path.GetPathRoot(destination);

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.TotalFreeSpace;
                }
            }

            return -1;
        }
    }
}
