using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hik.DataAccess.Abstractions
{
    public interface IBaseRepository<T>
        where T : class, IEntity
    {
        T Add(T entity);

        Task<List<T>> GetAllAsync();

        IQueryable<T> GetAll(params Expression<Func<T, object>>[] includes);


        Task<List<T>> GetLatestGroupedBy(Expression<Func<T, object>> groupBy);

        Task<List<T>> GetLatestGroupedBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, int>> groupBy);

        Task<List<T>> LastAsync(int last);

        IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);

        Task<T> FindByAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyByDescAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByDesc, int skip, int take, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyByAscAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByAsc, int skip, int top, params Expression<Func<T, object>>[] includes);

        Task<int> CountAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        void Update(T entity);

        void Remove(params object[] keys);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        void AddRange(IEnumerable<T> entities);
    }
}
