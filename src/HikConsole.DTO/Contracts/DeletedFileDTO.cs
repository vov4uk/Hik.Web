namespace HikConsole.DTO.Contracts
{
    public class DeletedFileDTO
    {
        public int Id { get; set; }

        public int CameraId { get; set; }

        public int JobId { get; set; }

        public string FilePath { get; set; }

        public string Extention { get; set; }
    }
}
