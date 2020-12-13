using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HikConsole.DataAccess
{
    public interface IBaseRepository<T> where T : class
    {
        ValueTask<EntityEntry<T>> AddAsync(T entity);

        Task AddRangeAsync(IEnumerable<T> entities);

        Task<List<T>> GetAllAsync();

        Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        Task<List<T>> LastAsync(int last);

        Task<T> FindByAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<bool> UpdateAsync(T entity);

        Task<bool> DeleteAsync(Expression<Func<T, bool>> identity, params Expression<Func<T, object>>[] includes);

        Task<bool> DeleteAsync(T entity);

    }
}
