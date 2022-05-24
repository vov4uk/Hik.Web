using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public class HikMappingProfile : Profile
    {
        public HikMappingProfile()
        {
            this.CreateMap<MediaFileDto, MediaFile>(MemberList.None)
                .ForMember(x => x.DownloadHistory, opt => opt.Ignore())
                .ForMember(x => x.DownloadDuration, opt => opt.Ignore())
                .ForMember(x => x.JobTriggerId, opt => opt.Ignore())
                .ForMember(x => x.JobTrigger, opt => opt.Ignore());

            this.CreateMap<MediaFile, MediaFileDto>(MemberList.None)
                .ForMember(x => x.DownloadDuration, opt => opt.MapFrom(x => x.DownloadDuration.Duration))
                .ForMember(x => x.Path, opt => opt.MapFrom(x => x.GetPath()));

            this.CreateMap<MediaFileDto, DownloadDuration>(MemberList.None)
                .ForMember(x => x.Duration, opt => opt.MapFrom(y => y.DownloadDuration))
                .ForMember(x => x.Started, opt => opt.MapFrom(y => y.DownloadStarted));

            this.CreateMap<JobTrigger, TriggerDto>(MemberList.None)
                .ForMember(x => x.JobTriggerId, opt => opt.MapFrom(y => y.Id))
                .ForMember(x => x.Name, opt => opt.MapFrom(y => y.TriggerKey));

            this.CreateMap<ExceptionLog, ExceptionLogDto>(MemberList.None);

            this.CreateMap<DailyStatistic, DailyStatisticDto>(MemberList.None);

            this.CreateMap<HikJob, HikJobDto>(MemberList.None)
                .ForMember(x => x.Error, opt => opt.MapFrom(y => y.ExceptionLog))
                .ForMember(x => x.JobTrigger, opt => opt.MapFrom(y => y.JobTrigger.TriggerKey));
        }
    }
}