using MediatR;

namespace Hik.Web.Queries.Statistic
{
    public class StatisticQuery : RequestBase, IRequest<IHandlerResult>
    {
        public int TriggerId { get; set; }
    }
}
