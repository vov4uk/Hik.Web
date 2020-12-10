namespace HikConsole.DTO.Contracts
{
    public class DeletedFileDTO
    {
        public DeletedFileDTO()
        {
        }
        
        public DeletedFileDTO(string fileName, string extention)
        {
            this.FileName = fileName;
            this.Extention = extention;
        }

        public string FileName { get; set; }

        public string Extention { get; set; }
    }
}
