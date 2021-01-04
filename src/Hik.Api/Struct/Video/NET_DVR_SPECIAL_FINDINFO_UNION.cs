using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Video
{
    [ExcludeFromCodeCoverage]
    [StructLayout(LayoutKind.Explicit)]
    internal struct NET_DVR_SPECIAL_FINDINFO_UNION
    {
        [FieldOffset(0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I1)]
        public byte[] byLenth;

        [FieldOffset(0)] public NET_DVR_ATMFINDINFO struATMFindInfo;
    }
}