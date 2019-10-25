using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HikConsole.Helpers
{
    public static class Utils
    {
        public static long DirSize(DirectoryInfo d)
        {
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
                size += DirSize(di);
            }

            return size;
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0,6:0.00} {1}", dblSByte, suffix[i]);
        }

        public static long GetTotalFreeSpace(string destenation)
        {
            var driveName = Path.GetPathRoot(destenation);

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.TotalFreeSpace;
                }
            }

            return -1;
        }

        public static FileInfo GetNewestFile(string folder)
        {
            var allFiles = GetFiles(folder);
            var maxFileName = allFiles.Max(o => o.FullName);
            return allFiles.FirstOrDefault(x => x.FullName == maxFileName);
        }

        public static FileInfo GetOldestFile(string folder)
        {
            var allFiles = GetFiles(folder);
            var minFileName = allFiles.Min(o => o.FullName);
            return allFiles.FirstOrDefault(x => x.FullName == minFileName);
        }

        private static IEnumerable<FileInfo> GetFiles(string folder)
        {
            return new DirectoryInfo(folder).GetFiles("*.mp4", SearchOption.AllDirectories);
        }
    }
}
