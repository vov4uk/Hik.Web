using AutoMapper;
using HikConsole.DataAccess.Data;
using HikConsole.DTO.Contracts;

namespace HikConsole.DataAccess
{
    public partial class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            this.CreateMap<CameraDTO, Camera>(MemberList.None);
            this.CreateMap<DeletedFileDTO, DeletedFile>(MemberList.None);
            this.CreateMap<HardDriveStatusDTO, HardDriveStatus>(MemberList.None);
            this.CreateMap<PhotoDTO, Photo>(MemberList.None);
            this.CreateMap<VideoDTO, Video>(MemberList.None);
        }
    }
}
