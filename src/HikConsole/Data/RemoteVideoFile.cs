using System;
using HikApi.Struct;

namespace HikConsole.Data
{
    public class RemoteVideoFile
    {
        public RemoteVideoFile()
        { 
        }

        public RemoteVideoFile(NET_DVR_FINDDATA_V30 findData)
        {
            this.Name = findData.sFileName;
            this.StartTime = findData.struStartTime.ToDateTime();
            this.StopTime = findData.struStopTime.ToDateTime();
            this.Size = findData.dwFileSize;
        }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }

        public long Size { get; set; }
    }
}