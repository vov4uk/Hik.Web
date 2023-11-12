using Hik.DTO.Config;
using MediatR;

namespace Hik.Web.Pages.Config
{
    public class ArchiveModel : ConfigModel<ArchiveConfig>
    {
        public ArchiveModel(IMediator mediator) : base(mediator) { }
    }
}
