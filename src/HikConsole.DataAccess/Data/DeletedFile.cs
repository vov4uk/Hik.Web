using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HikConsole.DataAccess.Data
{
    [Table("DeletedFiles")]
    public class DeletedFile : IAuditable
    {
        [Key] 
        public int Id { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [ForeignKey("Job")]
        public int JobId { get; set; }

        public string FilePath { get; set; }

        public string Extention { get; set; }

        public virtual Camera Camera { get; set; }

        public virtual Job Job { get; set; }
    }
}
