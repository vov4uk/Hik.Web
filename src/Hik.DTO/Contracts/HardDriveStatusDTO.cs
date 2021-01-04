namespace Hik.DTO.Contracts
{
    public class HardDriveStatusDTO
    {
        public uint Capacity { get; set; }

        public uint FreeSpace { get; set; }

        public uint HdStatus { get; set; }

        public byte HDAttr { get; set; }

        public byte HDType { get; set; }

        public byte Recycling { get; set; }

        public uint PictureCapacity { get; set; }

        public uint FreePictureSpace { get; set; }
    }
}
