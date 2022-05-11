using Hik.DataAccess.Abstractions;
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

        public IUnitOfWork CreateUnitOfWork()
        {
            var db = new DataContext(this.connection);
            db.Database.EnsureCreated();
            return new UnitOfWork<DataContext>(db);
        }
    }
}
