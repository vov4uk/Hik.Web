using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
            Context.Database.EnsureCreated();
        }

        public IBaseRepository<TEntity> GetRepository<TEntity>()
            where TEntity : class, IEntity
        {
            if (repositories == null) repositories = new ConcurrentDictionary<Type, object>();

            var type = typeof(TEntity);
            if (!repositories.ContainsKey(type))
            {
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File($"Logs\\SQL.txt")
                    .CreateLogger();


                repositories[type] = new BaseRepository<TEntity>(Context, logger);
            }
            return (IBaseRepository<TEntity>)repositories[type];
        }

        public TContext Context { get; }

        public Task<int> SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }
        
        public async Task<int> SaveChangesAsync(HikJob job)
        {
            this.ProcessAuditItems(job);
            var res = await this.SaveChangesAsync();
            return res;
        }

        public void SaveChanges()
        {
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
