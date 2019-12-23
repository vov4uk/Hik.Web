using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HikConsole.DataAccess
{
    public interface IBaseRepository<T> where T : class
    {
        ValueTask<EntityEntry<T>> Add(T entity);

        Task AddRange(IEnumerable<T> entities);

        Task<List<T>> GetAll();

        Task<List<T>> GetAll(params Expression<Func<T, object>>[] includes);

        Task<List<T>> Last(int last);

        Task<List<T>> SearchBy(Expression<Func<T, bool>> searchBy, params Expression<Func<T, object>>[] includes);

        Task<T> FindBy(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<bool> Update(T entity);

        Task<bool> Delete(Expression<Func<T, bool>> identity, params Expression<Func<T, object>>[] includes);

        Task<bool> Delete(T entity);

    }
}
