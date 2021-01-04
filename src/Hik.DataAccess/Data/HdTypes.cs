using System.ComponentModel;

namespace Hik.DataAccess.Data
{
    public enum HdTypes : byte
    {
        [Description("Local disk")]
        Local = 0,

        [Description("eSATA disk")]
        eSATA = 1,

        [Description("NFS disk")]
        NFS = 2,

        [Description("iSCSI disk")]
        iSCSI = 3,

        [Description("RAID virtual disk")]
        RAID = 4,

        [Description("SD card")]
        SDcard = 5,

        [Description("miniSAS")]
        miniSAS = 6,
    }
}
