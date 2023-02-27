using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.EntityFrameworkCore;

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

                var file = await filesRepo.FindByAsync(x => x.Id == request.FileId);

                if (file != null && filesHelper.FileExists(file.GetPath()))
                {
                    var beforeFile = filesRepo.GetAll()
                        .Where(x => x.JobTriggerId == file.JobTriggerId && x.Id < file.Id)
                        .OrderByDescending(x => x.Date)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();

                    MediaFileDto previousFile = default;
                    if (beforeFile != null)
                    {
                        previousFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDto>(beforeFile);
                    }

                    var afterFile = filesRepo.GetAll()
                        .Where(x => x.JobTriggerId == file.JobTriggerId && x.Id > file.Id)
                        .OrderBy(x => x.Date).ThenBy(x => x.Id)
                        .FirstOrDefault();

                    MediaFileDto nextFile = default;
                    if (afterFile != null)
                    {
                        nextFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDto>(afterFile);
                    }

                    var title = $"{file.Name} ({file.Duration.FormatSeconds()})";
                    var poster = await videoHelper.GetThumbnailStringAsync(file.GetPath(), 1080, 608).ConfigureAwait(false);

                    return new PlayDto
                    {
                        PreviousFile = previousFile,
                        CurrentFile = HikDatabase.Mapper.Map<MediaFile, MediaFileDto>(file),
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
                        Poster = videoHelper.DefaultPoster,
                        FileTitle = "Not found"
                    };
                }
            }
        }
    }
}
