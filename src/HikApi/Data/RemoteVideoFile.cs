using System;
using System.Diagnostics.CodeAnalysis;
using HikApi.Abstraction;
using HikApi.Struct.Video;

namespace HikApi.Data
{
    [ExcludeFromCodeCoverage]
    public class RemoteVideoFile : IRemoteFile
    {
        private const string StartDateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string EndDateTimePrintFormat = "HH:mm:ss";
        private const string TimeFormat = "HHmmss";
        private const string DateFormat = "yyyyMMdd_HHmmss";
        private const double BytesInMegaByte = 1024.0 * 1024.0;  

        public RemoteVideoFile()
        {
            // needed for Unit Tests, to be mocked
        }

        public RemoteVideoFile(NET_DVR_FINDDATA_V30 findData)
        {
            this.Name = findData.sFileName;
            this.StartTime = findData.struStartTime.ToDateTime();
            this.StopTime = findData.struStopTime.ToDateTime();
            this.Size = findData.dwFileSize;
        }

        public string Name { get; }

        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }

        public long Size { get; }

        public string ToUserFriendlyString()
        {
            return $"{this.Name} | {this.StartTime.ToString(StartDateTimePrintFormat)} - {this.StopTime.ToString(EndDateTimePrintFormat)} | {this.Size/BytesInMegaByte,6:0.00} MB ";
        }
        
        public string ToDirectoryNameString()
        {
            return $"{this.StartTime.Year:0000}-{this.StartTime.Month:00}\\{this.StartTime.Day:00}";
        }
        
        public string ToFileNameString()
        {
            return $"{this.StartTime.ToString(DateFormat)}_{this.StopTime.ToString(TimeFormat)}_{this.Name}.mp4";
        }
    }
}
