using Hik.DTO.Config;
using MediatR;

namespace Hik.Web.Pages.Config
{
    public class ImagesCollectorModel : ConfigModel<ImagesCollectorConfig>
    {
        public ImagesCollectorModel(IMediator mediator) : base(mediator) { }
    }
}
