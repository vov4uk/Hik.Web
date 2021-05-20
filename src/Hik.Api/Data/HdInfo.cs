using System;
using System.Collections.Generic;
using System.Text;
using Hik.Api.Struct.Config;

namespace Hik.Api.Data
{
    public class HdInfo
    {
        public HdInfo() { }
        internal HdInfo(NET_DVR_SINGLE_HD hd)
        {
            Capacity = hd.dwCapacity;
            FreeSpace = hd.dwFreeSpace;
            HdStatus = hd.dwHdStatus;
            HDAttr = hd.byHDAttr;
            HDType = hd.byHDType;
            Recycling = hd.byRecycling;
            PictureCapacity = hd.dwPictureCapacity;
            FreePictureSpace = hd.dwFreePictureSpace;
        }

        public bool IsErrorStatus => HdStatus == 2;

        public uint Capacity { get; set; }
        public uint FreeSpace { get; set; }
        public uint HdStatus { get; set; }
        public byte HDAttr { get; set; }
        public byte HDType { get; set; }
        public byte Recycling { get; set; }
        public uint PictureCapacity { get; set; }
        public uint FreePictureSpace { get; set; }

        public override string ToString()
        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine(GetRow(nameof(Capacity), ToGB(Capacity)));
            sb.AppendLine(GetRow(nameof(FreeSpace), ToGB(FreeSpace)));
            sb.AppendLine(GetRow(nameof(PictureCapacity), ToGB(PictureCapacity)));
            sb.AppendLine(GetRow(nameof(FreePictureSpace), ToGB(FreePictureSpace)));
            sb.AppendLine(GetRow(nameof(HdStatus), (HdStatuses.TryGetValue(HdStatus, out var status) ? status : "unknown")));
            sb.AppendLine(GetRow(nameof(HDAttr), (HdAttributes.TryGetValue(HDAttr, out var atr) ? atr : "unknown")));
            sb.AppendLine(GetRow(nameof(HDType), (HdTypes.TryGetValue(HDType, out var hdType) ? hdType : "unknown")));
            sb.AppendLine(GetRow(nameof(Recycling), Convert.ToString(Recycling)));

            return sb.ToString();
        }

        private static readonly Dictionary<uint, string> HdStatuses = new Dictionary<uint, string>
        {
            {0, "normal"},
            {1, "unformatted"},
            {2, "error"},
            {3, "S.M.A.R.T state"},
            {4, "not match"},
            {5, "sleeping"},
            {6, "unconnected(network disk)"},
            {7, "virtual disk is normal and supports expansion"},
            {10, "hard disk is being restored"},
            {11, "hard disk is being formatted"},
            {12, "hard disk is waiting formatted"},
            {13, "the hard disk has been uninstalled"},
            {14, "local hard disk does not exist"},
            {15, "it is deleting the network disk"},
            {16, "locked"}
        };
        
        private static readonly Dictionary<uint, string> HdAttributes = new Dictionary<uint, string>
        {
            {0, "default"},
            {1, "redundancy (back up important data)"},
            {2, "read only"},
            {3, "Archiving"},
            {4, "Cannot be read/read"}
        };   
        
        private static readonly Dictionary<uint, string> HdTypes = new Dictionary<uint, string>
        {
            {0, "local disk"},
            {1, "eSATA disk"},
            {2, "NFS disk"},
            {3, "iSCSI disk"},
            {4, "RAID virtual disk"},
            {5, "SD card"},
            {6, "miniSAS"}
        };

        private string ToGB(uint mb)
        {
            return $"{mb / 1024.0:0.00} GB ({mb} Mb)";
        }

        private string GetRow(string field, string value)
        {
            return $"{field,-24}: {value}";
        }
    }
}