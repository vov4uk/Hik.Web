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
            this.CreateMap<CameraConfig, CameraDTO>(MemberList.None);
            this.CreateMap<HikRemoteFile, FileDTO>(MemberList.None);
            this.CreateMap<FileDTO, HikRemoteFile>(MemberList.None);
        }
    }
}
