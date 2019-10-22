using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HikConsole.Helpers
{
    public static class Utils
    {
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> data, int count)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (count <= 0)
            {
                return data;
            }

            if (data is ICollection<T> collection)
            {
                return collection.Take(collection.Count - count);
            }

            IEnumerable<T> Skipper()
            {
                using (var enumer = data.GetEnumerator())
                {
                    T[] queue = new T[count];
                    int index = 0;

                    while (index < count && enumer.MoveNext())
                    {
                        queue[index++] = enumer.Current;
                    }

                    index = -1;
                    while (enumer.MoveNext())
                    {
                        index = (index + 1) % count;
                        yield return queue[index];
                        queue[index] = enumer.Current;
                    }
                }
            }

            return Skipper();
        }

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

        public static DateTime GetNewestFile(string folder)
        {
            return GetFiles(folder).Max(o => o.LastWriteTime);
        }

        public static DateTime GetOldestFile(string folder)
        {
            return GetFiles(folder).Min(o => o.LastWriteTime);
        }

        private static IEnumerable<FileInfo> GetFiles(string folder)
        {
            return new DirectoryInfo(folder).GetFiles("*.mp4", SearchOption.AllDirectories);
        }
    }
}
