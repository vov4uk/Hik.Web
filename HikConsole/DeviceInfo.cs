

namespace HikConsole
{
    public class DeviceInfo
    {
        public DeviceInfo()
        {
        }

        public DeviceInfo(SDK.NET_DVR_DEVICEINFO_V30 deviceInfo)
        {
            ChannelNumber = deviceInfo.byChanNum;
            StartChannel = deviceInfo.byStartChan;
        }

        public byte ChannelNumber { get; }

        public byte StartChannel { get; }
    }
}
