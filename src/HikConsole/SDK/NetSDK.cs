﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace HikConsole.SDK
{
    [ExcludeFromCodeCoverage]
    public class NetSDK
    {
        public const string DllPath = @"SDK\HCNetSDK";
#pragma warning disable SA1310
        public const int CARDNUM_LEN_OUT = 32;
        public const int GUID_LEN = 16;
        public const int SERIALNO_LEN = 48;
        public const int NET_DVR_PLAYSTART = 1;
        public const int NET_DVR_FILE_SUCCESS = 1000;
        public const int NET_DVR_FILE_NOFIND = 1001;
        public const int NET_DVR_ISFINDING = 1002;
        public const int NET_DVR_NOMOREFILE = 1003;
#pragma warning restore SA1310

        [DllImport(DllPath)]
        internal static extern int NET_DVR_FindFile_V40(int lUserID, ref NET_DVR_FILECOND_V40 pFindCond);

        [DllImport(DllPath)]
        internal static extern uint NET_DVR_GetLastError();

        [DllImport(DllPath)]
        internal static extern int NET_DVR_FindNextFile_V30(int lFindHandle, ref NET_DVR_FINDDATA_V30 lpFindData);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_StopGetFile(int lFileHandle);

        [DllImport(DllPath)]
        internal static extern int NET_DVR_GetDownloadPos(int lFileHandle);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_PlayBackControl_V40(int lPlayHandle, uint dwControlCode, IntPtr lpInBuffer, uint dwInValue, IntPtr lpOutBuffer, ref uint lPOutValue);

        [DllImport(DllPath)]
        internal static extern int NET_DVR_GetFileByName(int lUserID, string sDVRFileName, string sSavedFileName);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_Init();

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_SetLogToFile(int bLogEnable, string strLogDir, bool bAutoDel);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_Logout(int iUserID);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_FindClose_V30(int lFindHandle);

        [DllImport(DllPath)]
        internal static extern bool NET_DVR_Cleanup();

        [DllImport(DllPath)]
        internal static extern int NET_DVR_Login_V30(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref NET_DVR_DEVICEINFO_V30 lpDeviceInfo);

#pragma warning disable SA1307
        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_TIME
        {
            public int dwYear;
            public int dwMonth;
            public int dwDay;
            public int dwHour;
            public int dwMinute;
            public int dwSecond;

            public NET_DVR_TIME(DateTime dateTime)
            {
                this.dwYear = dateTime.Year;
                this.dwMonth = dateTime.Month;
                this.dwDay = dateTime.Day;
                this.dwHour = dateTime.Hour;
                this.dwMinute = dateTime.Minute;
                this.dwSecond = dateTime.Second;
            }

            public override string ToString()
            {
                return $"{this.dwYear:0000}-{this.dwMonth:00}-{this.dwDay:00}_{this.dwHour:00}:{this.dwMinute:00}:{this.dwSecond:00}";
            }

            public DateTime ToDateTime()
            {
                return new DateTime(this.dwYear, this.dwMonth, this.dwDay, this.dwHour, this.dwMinute, this.dwSecond);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_ATMFINDINFO
        {
            public byte byTransactionType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes;

            public uint dwTransationAmount;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct NET_DVR_SPECIAL_FINDINFO_UNION
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I1)]
            public byte[] byLenth;

            [FieldOffset(0)]
            public NET_DVR_ATMFINDINFO struATMFindInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_FILECOND_V40
        {
            public int lChannel;
            public uint dwFileType;
            public uint dwIsLocked;
            public uint dwUseCardNo;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CARDNUM_LEN_OUT, ArraySubType = UnmanagedType.I1)]
            public byte[] sCardNumber;

            public NET_DVR_TIME struStartTime;
            public NET_DVR_TIME struStopTime;
            public byte byDrawFrame;
            public byte byFindType;
            public byte byQuickSearch;
            public byte bySpecialFindInfoType;
            public uint dwVolumeNum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = GUID_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] byWorkingDeviceGUID;

            public NET_DVR_SPECIAL_FINDINFO_UNION uSpecialFindInfo;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NET_DVR_FINDDATA_V30
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string sFileName;

            public NET_DVR_TIME struStartTime;
            public NET_DVR_TIME struStopTime;
            public uint dwFileSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string sCardNum;

            public byte byLocked;
            public byte byFileType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DVR_DEVICEINFO_V30
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SERIALNO_LEN, ArraySubType = UnmanagedType.I1)]
            public byte[] sSerialNumber;

            public byte byAlarmInPortNum;
            public byte byAlarmOutPortNum;
            public byte byDiskNum;
            public byte byDVRType;
            public byte byChanNum;
            public byte byStartChan;
            public byte byAudioChanNum;
            public byte byIPChanNum;
            public byte byZeroChanNum;
            public byte byMainProto;
            public byte bySubProto;
            public byte bySupport;
            public byte bySupport1;
            public byte bySupport2;
            public byte bySupport3;
            public byte byMultiStreamProto;
            public byte byStartDChan;
            public byte byStartDTalkChan;
            public byte byHighDChanNum;
            public byte bySupport4;
            public byte byLanguageType;
            public ushort wDevType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9, ArraySubType = UnmanagedType.I1)]
            public byte[] byRes2;
        }
#pragma warning restore SA1307
    }
}