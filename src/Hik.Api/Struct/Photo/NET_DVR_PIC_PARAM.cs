using System;
using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Photo
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_PIC_PARAM
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDVRFileName;
        public IntPtr pSavedFileBuf;
        public uint dwBufLen;
        public IntPtr lpdwRetLen;
        public NET_DVR_ADDRESS struAddr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes;
    }
}