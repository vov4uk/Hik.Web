using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HikApi.Abstraction;
using HikApi.Data;

namespace HikApi.Struct.Video
{
    [ExcludeFromCodeCoverage]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NET_DVR_FINDDATA_V30 : ISourceFile
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

        public IRemoteFile ToRemoteFile()
        {
            return new RemoteVideoFile(this);
        }
    }
}