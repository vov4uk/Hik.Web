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
            this.CreateMap<CameraConfig, Camera>(MemberList.None);
            this.CreateMap<DeletedFileDTO, DeletedFile>(MemberList.None);
            this.CreateMap<PhotoDTO, Photo>(MemberList.None);
            this.CreateMap<VideoDTO, Video>(MemberList.None);
            this.CreateMap<FileDTO, MediaFile>(MemberList.None);
        }
    }
}
