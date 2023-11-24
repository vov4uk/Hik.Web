using Microsoft.EntityFrameworkCore;

namespace Hik.DataAccess.Abstractions
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork CreateUnitOfWork(QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll);
    }
}
