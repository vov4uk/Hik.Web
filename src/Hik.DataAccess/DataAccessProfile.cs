using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public class DataAccessProfile : Profile
    {
        public DataAccessProfile()
        {
            this.CreateMap<MediaFileDTO, MediaFile>(MemberList.None)
                .ForMember(x => x.DownloadHistory, opt => opt.Ignore())
                .ForMember(x => x.DownloadDuration, opt => opt.Ignore())
                .ForMember(x => x.JobTriggerId, opt => opt.Ignore())
                .ForMember(x => x.JobTrigger, opt => opt.Ignore());

            this.CreateMap<MediaFile, MediaFileDTO>(MemberList.None)
                .ForMember(x => x.DownloadDuration, opt => opt.MapFrom(x => x.DownloadDuration.Duration));

            this.CreateMap<MediaFileDTO, DownloadDuration>(MemberList.None)
                .ForMember(x => x.Duration, opt => opt.MapFrom(y => y.DownloadDuration))
                .ForMember(x => x.Started, opt => opt.MapFrom(y => y.DownloadStarted));

            this.CreateMap<ExceptionLog, ExceptionLogDto>(MemberList.None);
            this.CreateMap<DailyStatistic, DailyStatisticDto>(MemberList.None);
            this.CreateMap<HikJob, HikJobDto>(MemberList.None)
                .ForMember(x => x.Error, opt => opt.MapFrom(y => y.ExceptionLog))
                .ForMember(x => x.JobTrigger, opt => opt.MapFrom(y => y.JobTrigger.TriggerKey));
        }
    }
}