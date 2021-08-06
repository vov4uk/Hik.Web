using Hik.DTO.Contracts;
using System.Collections.Generic;

namespace Job.FileProviders
{
    public interface IFileProvider
    {
        IReadOnlyCollection<MediaFileDTO> GetNextBatch(int batchSize = 100);
    }
}
