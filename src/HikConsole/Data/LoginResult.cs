namespace HikConsole.Data
{
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