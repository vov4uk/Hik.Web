using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public partial class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            this.CreateMap<MediaFileDTO, MediaFile>(MemberList.None)
                .ForMember(x => x.DownloadHistory, opt => opt.Ignore())
                .ForMember(x => x.DownloadDuration, opt => opt.Ignore())
                .ForMember(x => x.JobTriggerId, opt => opt.Ignore())
                .ForMember(x => x.JobTrigger, opt => opt.Ignore())
                .ForMember(x => x.DeleteHistory, opt => opt.Ignore());

            this.CreateMap<MediaFileDTO, DownloadDuration>(MemberList.None)
                .ForMember(x => x.Duration, opt => opt.MapFrom(y => y.DownloadDuration))
                .ForMember(x => x.Started, opt => opt.MapFrom(y => y.DownloadStarted));
        }
    }
}
