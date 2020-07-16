using AutoMapper;
using HikApi.Data;
using HikConsole.Config;
using HikConsole.DTO.Contracts;

namespace HikConsole.Infrastructure
{
    public class HikConsoleProfile : Profile
    {
        public HikConsoleProfile()
        {
            this.CreateMap<RemoteVideoFile, VideoDTO>(MemberList.None);
            this.CreateMap<HdInfo, HardDriveStatusDTO>(MemberList.None);
            this.CreateMap<RemotePhotoFile, PhotoDTO>(MemberList.None);
            this.CreateMap<CameraConfig, CameraDTO>(MemberList.None);
        }
    }
}
