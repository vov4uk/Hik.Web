using AutoMapper;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.DataAccess
{
    public partial class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            this.CreateMap<BaseConfig, Camera>(MemberList.None);
            this.CreateMap<CameraConfig, Camera>(MemberList.None);
            this.CreateMap<FileDTO, MediaFile>(MemberList.None);
        }
    }
}
