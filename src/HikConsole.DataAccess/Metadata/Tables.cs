using System.Diagnostics.CodeAnalysis;

namespace HikConsole.DataAccess.Metadata
{
    [ExcludeFromCodeCoverage]
    public static class Tables
    {
        public const string Video = nameof(Video);
        public const string Photo = nameof(Photo);
        public const string Job = nameof(Job);
        public const string Camera = nameof(Camera);
        public const string HardDriveStatus = nameof(HardDriveStatus);
    }
}
