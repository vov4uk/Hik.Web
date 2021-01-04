namespace HikConsole.DTO.Contracts
{
    public class DeletedFileDTO : MediaFileBase
    {
        public DeletedFileDTO()
        {
        }
        
        public DeletedFileDTO(string fileName, string extention)
        {
            this.FilePath = fileName;
            this.Extention = extention;
        }

        public string FilePath { get; set; }

        public string Extention { get; set; }
    }
}
