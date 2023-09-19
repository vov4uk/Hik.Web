using System;
using System.Diagnostics.CodeAnalysis;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class Utils
    {
        private const string StartDateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string DateFormat = "yyyyMMdd_HHmmss";
        private const string TimeFormat = "HHmmss";
        private const string DefaultPath = "/tmp/sd/record/";
        private const string YiFilePathFormat = "yyyy'Y'MM'M'dd'D'HH'H'";
        private const string YiFileNameFormat = "mm'M00S'";
        private const string Yi720PFileNameFormat = "mm'M00S60'";
        private const string NA = "N/A";
        private const int SECOND = 1;
        private const int MINUTE = 60 * SECOND;
        private const int HOUR = 60 * MINUTE;
        private const int DAY = 24 * HOUR;
        private const int MONTH = 30 * DAY;
        private static readonly string[] Suffix = { "B", "KB", "MB", "GB", "TB" };

        public static string FormatBytes(this long bytes)
        {
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return $"{dblSByte,6:0.00} {Suffix[i]}";
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

        public static string FormatSeconds(this int? seconds)
        {
            var sec = (double)(seconds ?? -1);
            return sec.FormatSeconds();
        }

        public static string GetString(this DateTime? yourDate)
        {
            return !yourDate.HasValue ? NA : GetString(yourDate.Value);
        }

        public static string GetString(this DateTime yourDate)
        {
            string format = yourDate == yourDate.Date ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss";
            return yourDate.ToString(format);
        }

        public static string GetRelativeTime(this DateTime? yourDate)
        {
            return !yourDate.HasValue ? NA : GetRelativeTime(yourDate.Value);
        }

        public static string GetRelativeTime(this DateTime yourDate)
        {
            return GetRelativeTime(new TimeSpan(DateTime.Now.Ticks - yourDate.Ticks)) + " ago";
        }

        public static string GetRelativeTime(this TimeSpan ts)
        {
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second" : $"{ts.Seconds} seconds";
            }

            if (delta < 60 * MINUTE)
            {
                return $"{ts.Minutes} minutes";
            }

            if (delta < 24 * HOUR)
            {
                return $"{ts.Hours}h {ts.Minutes}m";
            }

            if (delta < 7 * DAY)
            {
                return $"{ts.Days}d {ts.Hours}h";
            }

            if (delta < 30 * DAY)
            {
                return $"{ts.Days} days";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month" : $"{months} months";
            }

            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year" : $"{years} years";
        }

        public static string ToDirectoryName(this DateTime date)
        {
            return $"{date.Year:0000}-{date.Month:00}\\{date.Day:00}\\{date.Hour:00}";
        }

        public static string ToVideoUserFriendlyString(this MediaFileDto file)
        {
            return $"{file.Name} | {file.Date.ToString(StartDateTimePrintFormat)} - {file.Duration} | {FormatBytes(file.Size)} ";
        }

        public static string ToPhotoFileNameString(this MediaFileDto file)
        {
            return $"{file.Date.ToString(DateFormat)}.jpg";
        }

        public static string ToVideoFileNameString(this MediaFileDto file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Date.AddSeconds((double)file.Duration).ToString(TimeFormat)}.mp4";
        }

        public static string ToYiFileNameString(this MediaFileDto file)
        {
            return $"{file.Date.ToString(DateFormat)}.mp4";
        }

        public static string ToYiFilePathString(this DateTime date, ClientType clientType)
        {
            var utcStart = date.ToUniversalTime();
            return $"{DefaultPath}{utcStart.ToString(YiFilePathFormat)}/{utcStart.ToString(GetFileNameFormat(clientType))}.mp4";
        }

        public static string ToArchiveFileString(this DateTime date, int duration, string ext)
        {
            string durationString = duration > 0 ? $"_{date.AddSeconds(duration).ToString(TimeFormat)}" : string.Empty;
            return $"{date.ToString(DateFormat)}{durationString}{ext}";
        }

        public static decimal SafeDivision(this decimal numerator, decimal denominator)
        {
            return (denominator == 0) ? 0 : numerator / denominator;
        }

        private static string GetFileNameFormat(ClientType clientType)
        {
            return clientType == ClientType.Yi ? YiFileNameFormat : Yi720PFileNameFormat;
        }
    }
}
