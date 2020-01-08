﻿using System.Runtime.InteropServices;
using HikApi.Abstraction;
using HikApi.Data;

namespace HikApi.Struct.Photo
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_FIND_PICTURE_V50 : ISourceFile
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HikConst.PICTURE_NAME_LEN)]
        public string sFileName;

        public NET_DVR_TIME struTime;
        public uint dwFileSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HikConst.CARDNUM_LEN_V30)]
        public string sCardNum;

        public byte byPlateColor;
        public byte byVehicleLogo;
        public byte byFileType;
        public byte byRecogResult;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HikConst.MAX_LICENSE_LEN)]
        public string sLicense;

        public byte byEventSearchStatus;
        public NET_DVR_ADDRESS struAddr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256, ArraySubType = UnmanagedType.I1)]
        public byte[] byRes;

        public NET_DVR_PIC_EXTRA_INFO_UNION uPicExtraInfo;

        public IRemoteFile ToRemoteFile()
        {
            return new RemotePhotoFile(this);
        }
    }
}