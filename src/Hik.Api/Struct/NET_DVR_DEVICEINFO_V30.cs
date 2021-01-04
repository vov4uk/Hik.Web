using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hik.Api.Struct
{
    [ExcludeFromCodeCoverage]
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_DEVICEINFO_V30
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = HikConst.SERIALNO_LEN, ArraySubType = UnmanagedType.I1)]
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
}