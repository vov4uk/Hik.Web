using Hik.Api.Struct.Photo;
using Hik.Api.Struct.Video;
using System;

namespace Hik.Api.Data
{
    public class HikRemoteFile 
    {

        internal HikRemoteFile(NET_DVR_FIND_PICTURE_V50 findData)
        {
            this.Name = findData.sFileName;
            this.Date = findData.struTime.ToDateTime();
            this.Size = findData.dwFileSize;
        }

        internal HikRemoteFile(NET_DVR_FINDDATA_V30 findData)
        {
            this.Name = findData.sFileName;
            this.Date = findData.struStartTime.ToDateTime();
            this.Duration = (int)(findData.struStopTime.ToDateTime() - findData.struStartTime.ToDateTime()).TotalSeconds;
            this.Size = findData.dwFileSize;
        }

        public HikRemoteFile()
        {
        }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public int Duration { get; set; }

        public long Size { get; set; }
    }
}
