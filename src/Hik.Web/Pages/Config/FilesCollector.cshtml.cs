using Hik.DTO.Config;
using MediatR;

namespace Hik.Web.Pages.Config
{
    public class FilesCollectorModel : ConfigModel<FilesCollectorConfig>
    {
        public FilesCollectorModel(IMediator mediator) : base(mediator) { }
    }
}
