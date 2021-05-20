using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Config
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_SINGLE_HD
    {
        public uint dwHDNo; 
        public uint dwCapacity; 
        public uint dwFreeSpace; 
        public uint dwHdStatus;
        public byte byHDAttr;
        public byte byHDType; 
        public byte byDiskDriver;
        public uint dwStorageType;
        public uint dwPictureCapacity;
        public uint dwFreePictureSpace;
        public uint dwHdGroup; 
        public byte byRecycling;

        public byte byRes1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 104, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes3;
    }
}