﻿using System.Diagnostics.CodeAnalysis;

namespace HikApi.Data
{
    [ExcludeFromCodeCoverage]
    public class Session
    {
        public Session(int userId, byte channelNumber)
        {
            this.UserId = userId;
            this.Device = new DeviceInfo { ChannelNumber = channelNumber};
        }

        public int UserId { get; }

        public DeviceInfo Device { get; }
    }
}
