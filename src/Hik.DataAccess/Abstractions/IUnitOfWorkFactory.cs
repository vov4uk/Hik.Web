using Microsoft.EntityFrameworkCore;

namespace Hik.DataAccess.Abstractions
{
    public interface IUnitOfWorkFactory
    {
        public IUnitOfWork CreateUnitOfWork(QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll);
    }
}
