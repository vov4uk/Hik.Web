using HikApi.Abstraction;
using HikApi.Struct.Photo;
using System;

namespace HikApi.Data
{
    public class RemotePhotoFile : IRemoteFile
    {
        private const string StartDateTimePrintFormat = "yyyy.MM.dd HH:mm:ss";
        private const string DateFormat = "yyyyMMdd_HHmmss";
        private const double BytesInKiloByte = 1024.0;

        internal RemotePhotoFile(NET_DVR_FIND_PICTURE_V50 findData)
        {
            this.Name = findData.sFileName;
            this.Date = findData.struTime.ToDateTime();
            this.Size = findData.dwFileSize;
        }

        public RemotePhotoFile()
        {
        }

        public string Name { get; }

        public DateTime Date { get; set; }

        public long Size { get; }

        public string ToUserFriendlyString()
        {
            return $"{this.Name} | {this.Date.ToString(StartDateTimePrintFormat)} | {this.Size / BytesInKiloByte,6:0.00} KB ";
        }

        public string ToDirectoryNameString()
        {
            return $"{this.Date.Year:0000}-{this.Date.Month:00}\\{this.Date.Day:00}\\{this.Date.Hour:00}";
        }

        public string ToFileNameString()
        {
            return $"{this.Date.ToString(DateFormat)}_{this.Name}.jpg";
        }
    }
}
