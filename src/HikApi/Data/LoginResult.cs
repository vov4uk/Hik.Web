using System.Diagnostics.CodeAnalysis;

namespace HikApi.Data
{
    [ExcludeFromCodeCoverage]
    public class LoginResult
    {
        public LoginResult(int userId, byte channelNumber, byte startChannel)
        {
            this.UserId = userId;
            this.Device = new DeviceInfo() { ChannelNumber = channelNumber, StartChannel = startChannel };
        }

        public int UserId { get; }

        public DeviceInfo Device { get; }
    }
}
