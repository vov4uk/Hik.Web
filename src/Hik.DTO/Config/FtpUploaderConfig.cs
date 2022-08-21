namespace Hik.DTO.Config
{
    public class FtpUploaderConfig : BaseConfig
    {
        public string RemoteFolder { get; set; }

        public DeviceConfig FtpServer { get; set; } = new DeviceConfig() { PortNumber = 21 };

        public string[] AllowedFileExtentions { get; set; } = { ".mp4", ".jpg", ".ini" };

    }
}
