namespace Hik.DataAccess
{
    public interface IUnitOfWorkFactory
    {
        public IUnitOfWork CreateUnitOfWork();
    }
}
