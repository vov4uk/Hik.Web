using AutoMapper;
using Hik.Api.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Infrastructure
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
