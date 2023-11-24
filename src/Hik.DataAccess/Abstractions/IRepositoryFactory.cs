namespace Hik.DataAccess.Abstractions
{
    internal interface IRepositoryFactory
    {
        IBaseRepository<T> GetRepository<T>()
            where T : class, IEntity, new();
    }
}
