using System.Runtime.InteropServices;

namespace HikApi.Struct.Config
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_HDCFG
    {
        public uint dwSize;
        public uint dwHDCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.MAX_DISKNUM_V30, ArraySubType = UnmanagedType.Struct)]
        public NET_DVR_SINGLE_HD[] struHDInfo;
    }
}