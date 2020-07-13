using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HikConsole.DataAccess
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly DbContext ctx;

        public BaseRepository(DbContext context)
        {
            ctx = context;
        }

        public virtual ValueTask<EntityEntry<T>> AddAsync(T entity)
        {
            return ctx.Set<T>().AddAsync(entity);
        }

        public virtual Task AddRangeAsync(IEnumerable<T> entities)
        {
            return ctx.Set<T>().AddRangeAsync(entities);
        }

        public virtual Task<List<T>> GetAllAsync()
        {
            return ctx.Set<T>().ToListAsync();
        }

        public virtual async Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            var result = ctx.Set<T>().Where(i => true);

            foreach (var includeExpression in includes)
                result = result.Include(includeExpression);

            return await result.ToListAsync();
        }

        public virtual Task<List<T>> LastAsync(int last)
        {
            return ctx.Set<T>().OrderByDescending(x=>x).Take(last).ToListAsync();
        }

        public virtual async Task<List<T>> SearchByAsync(Expression<Func<T, bool>> searchBy,
            params Expression<Func<T, object>>[] includes)
        {
            var result = ctx.Set<T>().Where(searchBy);

            foreach (var includeExpression in includes)
                result = result.Include(includeExpression);

            return await result.ToListAsync();
        }

        /// <summary>
        ///     Finds by predicate.
        ///     http://appetere.com/post/passing-include-statements-into-a-repository
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="includes">The includes.</param>
        /// <returns></returns>
        public virtual async Task<T> FindByAsync(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {
            var result = ctx.Set<T>().Where(predicate);

            foreach (var includeExpression in includes)
                result = result.Include(includeExpression);

            return await result.FirstOrDefaultAsync();
        }

        public virtual async Task<bool> UpdateAsync(T entity)
        {
            try
            {
                ctx.Set<T>().Attach(entity);
                ctx.Entry(entity).State = EntityState.Modified;

                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> identity,
            params Expression<Func<T, object>>[] includes)
        {
            var results = ctx.Set<T>().Where(identity);

            foreach (var includeExpression in includes)
                results = results.Include(includeExpression);
            try
            {
                ctx.Set<T>().RemoveRange(results);
                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public virtual async Task<bool> DeleteAsync(T entity)
        {
            ctx.Set<T>().Remove(entity);
            return await Task.FromResult(true);
        }
    }
}