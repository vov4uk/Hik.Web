using Hik.DTO.Config;
using MediatR;

namespace Hik.Web.Pages.Config
{
    public class CameraModel : ConfigModel<CameraConfig>
    {
        public CameraModel(IMediator mediator) : base(mediator) { }
    }
}
