using System.Runtime.InteropServices;

namespace Hik.Api.Struct
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_ADDRESS
    {
        public NET_DVR_IPADDR struIP;
        public uint wPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes;
    }
}