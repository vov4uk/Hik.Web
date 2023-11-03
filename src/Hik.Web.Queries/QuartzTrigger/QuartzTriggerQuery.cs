using MediatR;

namespace Hik.Web.Queries.QuartzTrigger
{
    public class QuartzTriggerQuery : IRequest<IHandlerResult>
    {
        public int Id {  get; set; }
    }
}
