namespace HikConsole.DataAccess
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        public IUnitOfWork CreateUnitOfWork(string connectionString)
        {
            return new UnitOfWork<DataContext>(new DataContext(connectionString));
        }
    }
}
