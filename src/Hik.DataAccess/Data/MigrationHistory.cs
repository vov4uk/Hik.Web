using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [Table(Tables.MigrationHistory)]
    public class MigrationHistory
    {
        [Key]
        public string ScriptName { get; set; }

        public DateTime ExecutionDate { get; set; }
    }
}
