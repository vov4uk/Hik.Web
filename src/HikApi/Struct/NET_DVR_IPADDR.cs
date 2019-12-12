using System.Runtime.InteropServices;

namespace HikApi.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NET_DVR_IPADDR
    {
        /// char[16]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string sIpV4;

        /// BYTE[128]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes;

        public void Init()
        {
            byRes = new byte[128];
        }
    }
}