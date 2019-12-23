using System;
using System.Threading.Tasks;
using HikConsole.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace HikConsole.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync();

        Task<int> SaveChangesAsync(HikJob job, Camera camera);
    }

    public interface IUnitOfWork<out TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
    }
}
