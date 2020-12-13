using AutoMapper;
using HikApi.Data;
using HikConsole.DTO.Config;
using HikConsole.DTO.Contracts;

namespace HikConsole.Infrastructure
{
    public class HikConsoleProfile : Profile
    {
        public HikConsoleProfile()
        {
            this.CreateMap<RemoteVideoFile, VideoDTO>(MemberList.None);
            this.CreateMap<HdInfo, HardDriveStatusDTO>(MemberList.None);
            this.CreateMap<RemotePhotoFile, PhotoDTO>()
                .ForMember(dest => dest.DateTaken, opt => opt.MapFrom(src => src.Date));
            this.CreateMap<CameraConfig, CameraDTO>(MemberList.None);
        }
    }
}
