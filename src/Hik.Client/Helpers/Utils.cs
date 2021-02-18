using System;
using System.Diagnostics.CodeAnalysis;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class Utils
    {
        public const string DateFormatMilisecconds = "yyyyMMdd_HHmmss.fff";
        private const string StartDateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string DateFormat = "yyyyMMdd_HHmmss";
        private const string DefaultPath = "/tmp/sd/record/";
        private const string YiFilePathFormat = "yyyy'Y'MM'M'dd'D'HH'H'";
        private const string YiFileNameFormat = "mm'M00S'";
        private const string Yi720pFileNameFormat = "mm'M00S60'";
        private const int SECOND = 1;
        private const int MINUTE = 60 * SECOND;
        private const int HOUR = 60 * MINUTE;
        private const int DAY = 24 * HOUR;
        private const int MONTH = 30 * DAY;

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

        public static string FormatSeconds(this double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            string str = string.Empty;

            if (seconds > 0 && seconds < 60)
            {
                str = time.ToString(@"ss' s'");
            }
            else if (seconds >= 60 && seconds <= 3600)
            {
                str = time.ToString(@"mm'm 'ss's'");
            }
            else if (seconds > 3600)
            {
                str = $"{(int)time.TotalHours:D2}h {time:mm'm 'ss's'}";
            }

            return str;
        }

        public static string GetRelativeTime(DateTime? yourDate)
        {
            if (!yourDate.HasValue)
            {
                return "N/A";
            }

            var ts = new TimeSpan(DateTime.Now.Ticks - yourDate.Value.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }

            if (delta < 2 * MINUTE)
            {
                return "a minute ago";
            }

            if (delta < 45 * MINUTE)
            {
                return ts.Minutes + " minutes ago";
            }

            if (delta < 90 * MINUTE)
            {
                return "an hour ago";
            }

            if (delta < 24 * HOUR)
            {
                return ts.Hours + " hours ago";
            }

            if (delta < 48 * HOUR)
            {
                return "yesterday";
            }

            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }

        public static string FormatSeconds(this int? seconds)
        {
            var sec = (double)(seconds ?? -1);
            return sec.FormatSeconds();
        }

        public static string ToPhotoDirectoryNameString(this DateTime date)
        {
            return $"{date.Year:0000}-{date.Month:00}\\{date.Day:00}\\{date.Hour:00}";
        }

        public static string ToPhotoUserFriendlyString(this MediaFileDTO file)
        {
            return $"{file.Name} | {file.Date.ToString(StartDateTimePrintFormat)} | {FormatBytes(file.Size)}";
        }

        public static string ToVideoUserFriendlyString(this MediaFileDTO file)
        {
            return $"{file.Name} | {file.Date.ToString(StartDateTimePrintFormat)} - {file.Duration} | {FormatBytes(file.Size)} ";
        }

        public static string ToPhotoFileNameString(this MediaFileDTO file)
        {
            return $"{file.Date.ToString(DateFormatMilisecconds)}.jpg";
        }

        public static string ToVideoDirectoryNameString(this MediaFileDTO file)
        {
            return $"{file.Date.Year:0000}-{file.Date.Month:00}\\{file.Date.Day:00}";
        }

        public static string ToYiDirectoryNameString(this MediaFileDTO file)
        {
            return $"{file.Date.Year:0000}-{file.Date.Month:00}\\{file.Date.Day:00}\\{file.Date.Hour:00}";
        }

        public static string ToVideoFileNameString(this MediaFileDTO file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Duration}_{file.Name}.mp4";
        }

        public static string ToYiFileNameString(this MediaFileDTO file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Name}.mp4";
        }

        public static string ToYiFilePathString(this DateTime date, ClientType clientType)
        {
            var utcStart = date.ToUniversalTime();
            return $"{DefaultPath}{utcStart.ToString(YiFilePathFormat)}/{utcStart.ToString(GetFileNameformat(clientType))}.mp4";
        }

        public static string ToArchiveFileString(this DateTime date, int duration, string ext)
        {
            string durationString = duration > 0 ? $"_{duration}" : string.Empty;
            return $"{date.ToString(DateFormat)}{durationString}{ext}";
        }

        private static string GetFileNameformat(ClientType clientType)
        {
            return clientType == ClientType.Yi ? YiFileNameFormat : Yi720pFileNameFormat;
        }
    }
}
