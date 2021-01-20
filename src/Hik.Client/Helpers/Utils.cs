using System;
using System.Diagnostics.CodeAnalysis;
using Hik.Api.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class Utils
    {
        private const string StartDateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string DateFormat = "yyyyMMdd_HHmmss";
        private const string DefaultPath = "/tmp/sd/record/";
        private const string YiFilePathFormat = "yyyy'Y'MM'M'dd'D'HH'H'";
        private const string YiFileNameFormat = "mm'M00S'";
        private const string Yi720pFileNameFormat = "mm'M00S60'";

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

            string str;

            if (seconds > 0 && seconds < 60)
            {
                str = time.ToString(@"ss' s'");
            }
            else if (seconds >= 60 && seconds <= 3600)
            {
                str = time.ToString(@"mm'm 'ss's'");
            }
            else
            {
                str = time.ToString(@"hh'h 'mm'm 'ss's'");
            }

            return str;
        }

        public static string ToPhotoDirectoryNameString(this DateTime date)
        {
            return $"{date.Year:0000}-{date.Month:00}\\{date.Day:00}\\{date.Hour:00}";
        }

        public static string ToPhotoUserFriendlyString(this FileDTO file)
        {
            return $"{file.Name} | {file.Date.ToString(StartDateTimePrintFormat)} | {FormatBytes(file.Size)}";
        }

        public static string ToVideoUserFriendlyString(this FileDTO file)
        {
            return $"{file.Name} | {file.Date.ToString(StartDateTimePrintFormat)} - {file.Duration} | {FormatBytes(file.Size)} ";
        }

        public static string ToPhotoFileNameString(this FileDTO file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Name}.jpg";
        }

        public static string ToVideoDirectoryNameString(this FileDTO file)
        {
            return $"{file.Date.Year:0000}-{file.Date.Month:00}\\{file.Date.Day:00}";
        }

        public static string ToYiDirectoryNameString(this FileDTO file)
        {
            return $"{file.Date.Year:0000}-{file.Date.Month:00}\\{file.Date.Day:00}\\{file.Date.Hour:00}";
        }

        public static string ToVideoFileNameString(this FileDTO file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Duration}_{file.Name}.mp4";
        }

        public static string ToYiFileNameString(this FileDTO file)
        {
            return $"{file.Date.ToString(DateFormat)}_{file.Name}.mp4";
        }

        public static string ToYiFilePathString(this DateTime date, ClientType clientType)
        {
            var utcStart = date.ToUniversalTime();
            return $"{DefaultPath}{utcStart.ToString(YiFilePathFormat)}/{utcStart.ToString(GetFileNameformat(clientType))}.mp4";
        }

        private static string GetFileNameformat(ClientType clientType)
        {
            return clientType == ClientType.Yi ? YiFileNameFormat : Yi720pFileNameFormat;
        }
    }
}
