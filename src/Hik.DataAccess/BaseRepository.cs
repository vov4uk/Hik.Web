using Hik.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class, IEntity
    {
        protected DbContext Database { get; }

        protected DbSet<TEntity> DbSet { get; }

        public BaseRepository(DbContext context)
        {
            this.Database = context;
            this.DbSet = this.Database.Set<TEntity>();
        }

        public virtual TEntity Add(TEntity entity)
        {
            EntityEntry<TEntity> ent = null;
            using (var transaction = Database.Database.BeginTransaction())
            {
                try
                {
                    ent = DbSet.Add(entity);
                    Database.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

            return ent?.Entity;
        }

        public virtual Task<List<TEntity>> GetAllAsync()
        {
            return DbSet.ToListAsync();
        }

        public virtual IQueryable<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(i => true);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }
            Log.Debug($"GetAll : {result?.ToQueryString()}");
            return result;
        }

        public async Task<List<TEntity>> GetLatestGroupedBy(
            Expression<Func<TEntity, object>> groupBy)
        {
            var idsQuery = DbSet
                .GroupBy(groupBy)
                .Select(p => p.Max(x => x.Id));

            var ids = await idsQuery.ToListAsync();

            var result = DbSet.Where(p => ids.Contains(p.Id));
            Log.Debug($"GetLatestGroupedBy - 1 Ids: {idsQuery?.ToQueryString()}");
            Log.Debug($"GetLatestGroupedBy - 1 Result: {result?.ToQueryString()}");
            return await result?.ToListAsync();
        }

        public async Task<List<TEntity>> GetLatestGroupedBy(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, int>> groupBy)
        {
            var idsQuery = DbSet
                .Where(predicate)
                .GroupBy(groupBy)
                .Select(p => p.Max(x => x.Id));
            var ids = await idsQuery.ToListAsync();

            var result = DbSet.Where(p => ids.Contains(p.Id));
            Log.Debug($"GetLatestGroupedBy - 2 Ids : {idsQuery?.ToQueryString()}");
            Log.Debug($"GetLatestGroupedBy - 2 Result: {result?.ToQueryString()}");
            return await result?.ToListAsync();
        }

        public virtual Task<List<TEntity>> LastAsync(int last)
        {
            var result = DbSet.OrderByDescending(x => x).Take(last);
            Log.Debug($"LastAsync : {result?.ToQueryString()}");
            return result?.ToListAsync();
        }

        public virtual Task<TEntity> FindByAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            Log.Debug($"FindByAsync : {result?.ToQueryString()}");

            return result?.FirstOrDefaultAsync();
        }

        public virtual IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate)
        {
            var query = DbSet.Where(predicate);

            Log.Debug($"FindByAsync : {query?.ToQueryString()}");

            return query;
        }

        public virtual Task<List<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            Log.Debug($"FindManyAsync : {result?.ToQueryString()}");

            return result?.ToListAsync();
        }

        public virtual Task<List<TEntity>> FindManyByDescAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> orderByDesc,
            int skip,
            int take,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            result = result.OrderByDescending(orderByDesc).Skip(skip).Take(take);

            Log.Debug($"FindManyByDescAsync : {result?.ToQueryString()}");

            return result?.ToListAsync();
        }

        public virtual Task<List<TEntity>> FindManyByAscAsync(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> orderByAsc, int skip, int top,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            result = result.OrderBy(orderByAsc).Skip(skip).Take(top);

            Log.Debug($"FindManyByAscAsync : {result?.ToQueryString()}");

            return result?.ToListAsync();
        }

        public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }
            Log.Debug($"CountAsync : {result?.ToQueryString()}");
            return result?.CountAsync() ?? Task.FromResult(-1);
        }

        public void Update(TEntity entity)
        {
            using (var transaction = Database.Database.BeginTransaction())
            {
                try
                {
                    DbSet.Update(entity);
                    Database.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        public void Remove(params object[] keys)
        {
            TEntity entity = this.DbSet.Find(keys);
            this.Remove(entity);
        }

        public void Remove(TEntity entity)
        {
            if (this.Database.Entry(entity).State == EntityState.Detached)
            {
                this.DbSet.Attach(entity);
            }

            this.DbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities.Where(e => this.Database.Entry(e).State == EntityState.Detached))
            {
                this.DbSet.Attach(entity);
            }

            this.DbSet.RemoveRange(entities);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            this.DbSet.AddRange(entities);
        }
    }
}