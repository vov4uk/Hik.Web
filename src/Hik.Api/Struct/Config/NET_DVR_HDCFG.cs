using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Config
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_HDCFG
    {
        public uint dwSize;
        public uint dwHDCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.MAX_DISKNUM_V30)]
        public NET_DVR_SINGLE_HD[] struHDInfo;
    }
}