namespace Hik.DataAccess
{
    interface IRepositoryFactory
    {
        IBaseRepository<T> GetRepository<T>() where T : class;
    }
}
