using System.ComponentModel;

namespace HikConsole.DataAccess.Data
{
    public enum HdStatus : uint
    {
        [Description("Normal")]
        Normal = 0,

        [Description("Unformatted")]
        Unformatted = 1,

        [Description("Error")]
        Error = 2,

        [Description("S.M.A.R.T state")]
        Smart = 3,

        [Description("Not match")]
        NotMatch = 4,

        [Description("Sleeping")]
        Sleeping = 5,

        [Description("Unconnected(network disk)")]
        Unconnected = 6,

        [Description("Virtual disk is normal and supports expansion")]
        VirtualDisk = 7,

        [Description("Hard disk is being restored")]
        BeingRestored = 10,

        [Description("Hard disk is being formatted")]
        BeingFormatted = 11,

        [Description("Hard disk is waiting formatted")]
        WaitingFormatted = 12,

        [Description("Hard disk has been uninstalled")]
        Uninstalled = 13,

        [Description("Local hard disk does not exist")]
        NotExist = 14,

        [Description("It is deleting the network disk")]
        Deleting = 15,

        [Description("Locked")]
        Locked = 16
    }
}
