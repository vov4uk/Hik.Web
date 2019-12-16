using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HikConsole.DataAccess
{
    public class UnitOfWork<TContext> : IRepositoryFactory, IUnitOfWork<TContext>
        where TContext : DbContext, IDisposable
    {
        private Dictionary<Type, object> repositories;
        private bool disposedValue;

        public UnitOfWork(TContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            if (repositories == null) repositories = new Dictionary<Type, object>();

            var type = typeof(TEntity);
            if (!repositories.ContainsKey(type)) repositories[type] = new BaseRepository<TEntity>(Context);
            return (IBaseRepository<TEntity>)repositories[type];
        }

        public TContext Context { get; }

        public Task<int> SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    Context?.Dispose();
                }

                this.disposedValue = true;
            }
        }
    }
}
