using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hik.DataAccess.Abstractions
{
    public interface IBaseRepository<T> where T : class
    {
        ValueTask<T> AddAsync(T entity);

        EntityEntry<T> Add(T entity);

        Task AddRangeAsync(IEnumerable<T> entities);

        Task<List<T>> GetAllAsync();

        Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        Task<List<T>> LastAsync(int last);

        Task<T> FindByAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> keySelector, int skip, int top, params Expression<Func<T, object>>[] includes);

        Task<int> CountAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        void Update(T entity);

        void Remove(params object[] keys);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        void AddRange(IEnumerable<T> entities);
    }
}
