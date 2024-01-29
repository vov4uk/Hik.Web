using MediatR;

namespace Hik.Web.Queries.Dashboard
{
    public class DashboardQuery : IRequest<IHandlerResult>
    {
        public DateTime Day { get; set; }
    }
}
