using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Hik.Web.Queries.Play
{
    public class PlayQueryHandler : QueryHandler<PlayQuery>
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly IFilesHelper filesHelper;
        private readonly IVideoHelper videoHelper;

        public PlayQueryHandler(IUnitOfWorkFactory factory, IFilesHelper filesHelper, IVideoHelper videoHelper)
        {
            this.factory = factory;
            this.filesHelper = filesHelper;
            this.videoHelper = videoHelper;
        }

        protected override async Task<IHandlerResult> HandleAsync(PlayQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<MediaFile> filesRepo = uow.GetRepository<MediaFile>();

                var current = await GetMediaFilesAsync(filesRepo,
                    x => x.Id == request.FileId);

                if (current.Any() && filesHelper.FileExists(current[0].GetPath()))
                {
                    var file = current.First();

                    MediaFile? beforeFile = await filesRepo.GetAll()
                        .Where(x => x.JobTriggerId == file.JobTriggerId && x.Id < file.Id)
                        .OrderByDescending(x => x.Date).FirstOrDefaultAsync();

                    MediaFileDTO previousFile = default;
                    if (beforeFile != null)
                    {
                        previousFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDTO>(beforeFile);
                    }

                    var afterFile = await GetMediaFilesAsync(filesRepo,
                        x => x.JobTriggerId == file.JobTriggerId && x.Id > file.Id);

                    MediaFileDTO nextFile = default;
                    if (afterFile.Any())
                    {
                        nextFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDTO>(afterFile.First());
                    }

                    var title = $"{file.Name} ({file.Duration.FormatSeconds()})";
                    var poster = await videoHelper.GetThumbnailStringAsync(file.GetPath()).ConfigureAwait(false);

                    return new PlayDto
                    {
                        PreviousFile = previousFile,
                        CurrentFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDTO>(file),
                        NextFile = nextFile,
                        FileTitle = title,
                        Poster = poster,
                        FileTo = file.Date.AddSeconds(file.Duration ?? 0).ToString(Consts.DisplayDateTimeStringFormat)
                    };
                }
                else
                {
                    return new PlayDto
                    {
                        Poster = "http://vjs.zencdn.net/v/oceans.png",
                        FileTitle = "Not found"
                    };
                }
            }
        }

        private Task<List<MediaFile>> GetMediaFilesAsync(IBaseRepository<MediaFile> filesRepo, Expression<Func<MediaFile, bool>> predicate)
        {
            return filesRepo.FindManyAsync(
                        predicate,
                        x => x.Date,
                        skip: 0,
                        top: 1);
        }
    }
}
