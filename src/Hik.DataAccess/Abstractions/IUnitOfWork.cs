using System;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Hik.DataAccess.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<TEntity> GetRepository<TEntity>()
            where TEntity : class, IEntity, new();

        void SaveChanges();
        void SaveChanges(HikJob job);
    }

    public interface IUnitOfWork<out TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
    }
}
