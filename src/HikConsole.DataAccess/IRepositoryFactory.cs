namespace HikConsole.DataAccess
{
    interface IRepositoryFactory
    {
        IBaseRepository<T> GetRepository<T>() where T : class;
    }
}
