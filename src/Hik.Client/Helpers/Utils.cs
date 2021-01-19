using System;
using System.Diagnostics.CodeAnalysis;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class Utils
    {
        public static string FormatBytes(this long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return $"{dblSByte,6:0.00} {suffix[i]}";
        }

        public static string ToDirectoryNameString(this DateTime date)
        {
            return $"{date.Year:0000}-{date.Month:00}\\{date.Day:00}\\{date.Hour:00}";
        }
    }
}
