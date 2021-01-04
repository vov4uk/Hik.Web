using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Photo
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_PIC_EXTRA_INFO_UNION
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 544, ArraySubType = UnmanagedType.I1)]
        public byte[] byUnionLen;
    }
}