using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.DTO.Contracts;

namespace Hik.Client.FileProviders
{
    public interface IFileProvider
    {
        void Initialize(string[] directories);

        IReadOnlyCollection<MediaFileDto> GetNextBatch(string fileExtention, int batchSize = 100);

        IReadOnlyCollection<MediaFileDto> GetFilesOlderThan(string fileExtention, DateTime date);

        Task<IReadOnlyCollection<MediaFileDto>> GetOldestFilesBatch(bool readDuration = false);
    }
}
