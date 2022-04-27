using Hik.DataAccess.Abstractions;

namespace Hik.DataAccess
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly string connectionString;

        public UnitOfWorkFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IUnitOfWork CreateUnitOfWork()
        {
            var db = new DataContext(this.connectionString);
            db.Database.EnsureCreated();
            return new UnitOfWork<DataContext>(db);
        }
    }
}
