using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class UnitOfWork<TContext> : IRepositoryFactory, IUnitOfWork<TContext>
        where TContext : DbContext, IDisposable
    {
        private ConcurrentDictionary<Type, object> repositories;
        private bool disposedValue;

        public UnitOfWork(TContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IBaseRepository<TEntity> GetRepository<TEntity>()
            where TEntity : class, IEntity
        {
            if (repositories == null) repositories = new ConcurrentDictionary<Type, object>();

            var type = typeof(TEntity);
            if (!repositories.TryGetValue(type, out object value))
            {
                value = new BaseRepository<TEntity>(Context);
                repositories[type] = value;
            }
            return (IBaseRepository<TEntity>)value;
        }

        public TContext Context { get; }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        public void SaveChanges(HikJob job)
        {
            this.ProcessAuditItems(job);
            Context.SaveChanges();
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

        private void ProcessAuditItems(HikJob job)
        {
            foreach (var entity in this.Context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                .Select(e => e.Entity)
                .OfType<IAuditable>())
            {
                entity.JobId = job.Id;
            }
        }
    }
}
