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
            return new UnitOfWork<DataContext>(new DataContext(this.connectionString));
        }
    }
}
