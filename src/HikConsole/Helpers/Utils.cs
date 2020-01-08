using System.Diagnostics.CodeAnalysis;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class Utils
    {
        public static string FormatBytes(long bytes)
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
    }
}
