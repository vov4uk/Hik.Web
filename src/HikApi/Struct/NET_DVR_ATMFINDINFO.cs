using System.Runtime.InteropServices;

namespace HikApi.Struct
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NET_DVR_ATMFINDINFO
    {
        public byte byTransactionType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes;

        public uint dwTransationAmount;
    }
}
