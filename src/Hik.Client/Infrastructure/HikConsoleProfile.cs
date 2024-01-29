using AutoMapper;
using Dahua.Api.Data;
using Hik.Api.Data;
using Hik.DTO.Contracts;

namespace Hik.Client.Infrastructure
{
    public class HikConsoleProfile : Profile
    {
        public HikConsoleProfile()
        {
            this.CreateMap<HikRemoteFile, MediaFileDto>(MemberList.None);
            this.CreateMap<RemoteFile, MediaFileDto>(MemberList.None);
            this.CreateMap<MediaFileDto, HikRemoteFile>(MemberList.None);
        }
    }
}
