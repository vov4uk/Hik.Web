using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hik.DataAccess.Abstractions
{
    public interface IBaseRepository<T>
        where T : class, IEntity, new()
    {
        T Add(T entity);

        void AddRange(IEnumerable<T> entities);

        Task<int> CountAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> ExecuteQueryAsync(string query);

        IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);

        Task<T> FindByAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        T FindById(int id);

        Task<T> FindByIdAsync(int id);

        Task<List<T>> FindManyAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyByAscAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByAsc, int skip, int top, params Expression<Func<T, object>>[] includes);

        Task<List<T>> FindManyByDescAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderByDesc, int skip, int take, params Expression<Func<T, object>>[] includes);

        IQueryable<T> GetAll(params Expression<Func<T, object>>[] includes);

        Task<List<T>> GetAllAsync();
        Task<List<T>> GetLatestGroupedBy(Expression<Func<T, object>> groupBy);

        Task<List<T>> GetLatestGroupedBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, int>> groupBy);

        Task<List<T>> LastAsync(int last);
        void Remove(params object[] keys);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        void Update(T entity);
    }
}
