using System.Diagnostics.CodeAnalysis;

namespace HikApi.Data
{
    [ExcludeFromCodeCoverage]
    public class DeviceInfo
    {
        public byte ChannelNumber { get; set; }

        public byte StartChannel { get; set; }
    }
}