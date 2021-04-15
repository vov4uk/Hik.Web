using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess.Metadata
{
    [ExcludeFromCodeCoverage]
    public static class Tables
    {
        public const string Video = nameof(Video);
        public const string Photo = nameof(Photo);
        public const string MediaFile = nameof(MediaFile);
        public const string Job = nameof(Job);
        public const string JobTrigger = nameof(JobTrigger);
        public const string ExceptionLog = nameof(ExceptionLog);
        public const string DailyStatistics = nameof(DailyStatistics);
        public const string DeleteHistory = nameof(DeleteHistory);
        public const string DownloadHistory = nameof(DownloadHistory);
        public const string DownloadDuration = nameof(DownloadDuration);
    }
}
