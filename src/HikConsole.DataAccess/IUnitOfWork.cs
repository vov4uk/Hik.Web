using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HikConsole.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync();
    }

    public interface IUnitOfWork<out TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
    }
}
