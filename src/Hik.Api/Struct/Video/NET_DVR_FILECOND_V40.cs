using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hik.Api.Struct.Video
{
    [ExcludeFromCodeCoverage]
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_FILECOND_V40
    {
        public int lChannel;
        public uint dwFileType;

        public uint dwIsLocked; //Is it locked: 0-unlocked file, 1-locked file, 0xff means all files (including locked and unlocked)

        public uint dwUseCardNo;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.CARDNUM_LEN_OUT, ArraySubType = UnmanagedType.I1)]
        public byte[] sCardNumber;

        public NET_DVR_TIME struStartTime;
        public NET_DVR_TIME struStopTime;
        public byte byDrawFrame;
        public byte byFindType;
        public byte byQuickSearch;
        public byte bySpecialFindInfoType;
        public uint dwVolumeNum;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.GUID_LEN, ArraySubType = UnmanagedType.I1)]
        public byte[] byWorkingDeviceGUID;

        public NET_DVR_SPECIAL_FINDINFO_UNION uSpecialFindInfo;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes2;
    }
}