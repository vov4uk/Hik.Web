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
            this.CreateMap<HikRemoteFile, MediaFileDTO>(MemberList.None);
            this.CreateMap<MediaFileDTO, HikRemoteFile>(MemberList.None);
        }
    }
}
