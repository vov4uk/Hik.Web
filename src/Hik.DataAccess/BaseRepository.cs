using Hik.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        public virtual async ValueTask<TEntity> AddAsync(TEntity entity)
        {
            var result = await DbSet.AddAsync(entity);
            return result.Entity;
        }

        public virtual EntityEntry<TEntity> Add(TEntity entity)
        {
            return DbSet.Add(entity);
        }

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            return DbSet.AddRangeAsync(entities);
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

            return result;
        }

        public async Task<List<TEntity>> GetLatestGroupedBy(
            Expression<Func<TEntity, object>> groupBy)
        {
            var ids = await DbSet
                .GroupBy(groupBy)
                .Select(p => p.Max(x => x.Id))
                .ToListAsync();

            var result = DbSet.Where(p => ids.Contains(p.Id));
            return await result?.ToListAsync();
        }

        public async Task<List<TEntity>> GetLatestGroupedBy(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, int>> groupBy)
        {
            var ids = await DbSet
                .Where(predicate)
                .GroupBy(groupBy)
                .Select(p => p.Max(x => x.Id))
                .ToListAsync();

            var result = DbSet.Where(p => ids.Contains(p.Id));
            return await result?.ToListAsync();
        }

        public virtual Task<List<TEntity>> LastAsync(int last)
        {
            return DbSet.OrderByDescending(x => x).Take(last).ToListAsync();
        }

        public virtual async Task<TEntity> FindByAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            return await result?.FirstOrDefaultAsync();
        }

        public virtual async Task<List<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            return await result?.ToListAsync();
        }

        public virtual async Task<List<TEntity>> FindManyByDescAsync(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> orderByDesc, int skip, int top,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            result = result.OrderByDescending(orderByDesc).Skip(skip).Take(top);

            return await result?.ToListAsync();
        }

        public virtual async Task<List<TEntity>> FindManyByAscAsync(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> orderByAsc, int skip, int top,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            result = result.OrderBy(orderByAsc).Skip(skip).Take(top);

            return await result?.ToListAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            return await result?.CountAsync();
        }

        public void Update(TEntity entity)
        {
            DbSet.Attach(entity);
            Database.Entry(entity).State = EntityState.Modified;
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