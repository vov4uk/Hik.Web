using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Hik.Web.Queries.Search
{
    public class SearchQueryHandler : QueryHandler<SearchQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public SearchQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(SearchQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<MediaFile> filesRepo = uow.GetRepository<MediaFile>();

                var latestFile = await GetMediaFilesAsync(filesRepo,
                    x => x.JobTriggerId == request.JobTriggerId && x.Date <= request.DateTime,
                    top : 1);

                if (latestFile.Any())
                {
                    var file = latestFile.First();
                    var msg = (file.Date <= request.DateTime && request.DateTime <= file.Date.AddSeconds(file.Duration ?? 0)) ? "Match" : "Out of range";

                    var beforeFiles = await GetMediaFilesAsync(filesRepo,
                        x => x.JobTriggerId == request.JobTriggerId && x.Id < file.Id);

                    var files = await filesRepo.FindManyByAscAsync(
                        x => x.JobTriggerId == request.JobTriggerId && x.Id > file.Id,
                        x => x.Date,
                        skip: 0,
                        top: 5,
                        includes: x => x.DownloadDuration);
                    var afterFiles = files.ConvertAll(y => HikDatabase.Mapper.Map<MediaFile, MediaFileDto>(y));

                    return new SearchDto
                    {
                        BeforeRange = new List<MediaFileDto>(beforeFiles.OrderBy(x => x.Date)),
                        InRange = latestFile,
                        AfterRange = new List<MediaFileDto>(afterFiles),
                        Message = msg
                    };
                }
                else
                {
                    var latest = await GetMediaFilesAsync(filesRepo,
                        x => x.JobTriggerId == request.JobTriggerId);

                    return new SearchDto
                    {
                        InRange = new List<MediaFileDto>(latest.OrderBy(x => x.Date)),
                        Message = "Latest 5 files"
                    };
                }
            }
        }

        private static async Task<List<MediaFileDto>> GetMediaFilesAsync(IBaseRepository<MediaFile> filesRepo, Expression<Func<MediaFile, bool>> predicate, int top = 5)
        {
            var files = await filesRepo.FindManyByDescAsync(
                        predicate,
                        x => x.Date,
                        skip: 0,
                        top: top,
                        includes : x => x.DownloadDuration);
            return files.ConvertAll(y => HikDatabase.Mapper.Map<MediaFile, MediaFileDto>(y));
        }
    }
}
