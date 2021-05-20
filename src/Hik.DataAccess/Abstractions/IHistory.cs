using Hik.DataAccess.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Abstractions
{
    public interface IHistory
    {
        [ForeignKey(nameof(MediaFile))]
        int MediaFileId { get; set; }

        MediaFile MediaFile { get; set; }
    }
}
