using Hik.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbConfiguration connection;

        public UnitOfWorkFactory(IDbConfiguration connection)
        {
            this.connection = connection;
        }

        public IUnitOfWork CreateUnitOfWork(QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll)
        {
            var db = new DataContext(this.connection);
            db.Database.EnsureCreated();
            db.ChangeTracker.QueryTrackingBehavior = trackingBehavior;
            return new UnitOfWork<DataContext>(db);
        }
    }
}
