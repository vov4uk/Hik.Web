using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Photo
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_FIND_PICTURE_PARAM
    {
        public uint dwSize;
        public int lChannel;
        public byte byFileType;
        public byte byNeedCard;
        public byte byProvince;
        public byte byRes1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.CARDNUM_LEN_V30, ArraySubType = UnmanagedType.I1)]
        public byte[] sCardNum;

        public NET_DVR_TIME struStartTime;
        public NET_DVR_TIME struStopTime;
        public int dwTrafficType;
        public int dwVehicleType;
        public int dwIllegalType;
        public byte byLaneNo;
        public byte bySubHvtType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes2;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HikConst.CARDNUM_LEN_V30)]
        public string sLicense;

        public byte byRegion;
        public byte byCountry;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes3;
    }
}