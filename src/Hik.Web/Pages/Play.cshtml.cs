using Hik.Web.Queries.Play;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class PlayModel : PageModel
    {
        private readonly IMediator mediator;

        public PlayModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PlayDto Dto { get; private set; }

        public async Task<IActionResult> OnGetAsync(int fileId)
        {
            Dto = await mediator.Send(new PlayQuery() { FileId = fileId }) as PlayDto;
            return Page();
        }
    }
}