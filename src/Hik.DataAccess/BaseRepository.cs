using Hik.DataAccess.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Hik.DataAccess
{
    [ExcludeFromCodeCoverage]
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class, IEntity, new()
    {
        public BaseRepository(DbContext context)
        {
            this.Database = context;
            this.DbSet = this.Database.Set<TEntity>();
        }

        protected DbContext Database { get; }

        protected DbSet<TEntity> DbSet { get; }
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

        public void AddRange(IEnumerable<TEntity> entities)
        {
            this.DbSet.AddRange(entities);
        }

        public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            LogQuery(nameof(CountAsync), result);

            return result?.CountAsync() ?? Task.FromResult(-1);
        }

        public async Task<List<TEntity>> ExecuteQueryAsync(string query)
        {
            using (var command = Database.Database.GetDbConnection().CreateCommand())
            {
                Log.Information(query);
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                await Database.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var lst = new List<TEntity>();
                    while (await reader.ReadAsync())
                    {
                        var newObject = new TEntity();
                        foreach (PropertyInfo property in newObject.GetType().GetProperties())
                        {
                            ColumnAttribute customAttribute = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute)) as ColumnAttribute;
                            if (customAttribute != null)
                            {
                                int ordinal = reader.GetOrdinal(customAttribute.Name);
                                object obj = ordinal != -1 ?
                                    reader.GetValue(ordinal) :
                                    throw new NullReferenceException(string.Format("Class [{0}] have attribute of field [{1}] which not exist in reader", this.GetType(), customAttribute.Name));

                                if (obj != DBNull.Value)
                                {
                                    property.SetValue(newObject, Unbox(obj, property.PropertyType), null);
                                }
                            }
                        }
                        lst.Add(newObject);
                    }

                    return lst;
                }
            }
        }

        public virtual IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate)
        {
            var query = DbSet.Where(predicate);

            LogQuery(nameof(FindBy), query);
            return query;
        }

        public virtual Task<TEntity> FindByAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }
            LogQuery(nameof(FindByAsync), result);

            return result?.FirstOrDefaultAsync();
        }

        public TEntity FindById(int id)
        {
            return DbSet.First(x => x.Id == id);
        }

        public Task<TEntity> FindByIdAsync(int id)
        {
            return DbSet.FirstAsync(x => x.Id == id);
        }

        public virtual Task<List<TEntity>> FindManyAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(predicate);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }

            LogQuery(nameof(FindManyAsync), result);

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

            LogQuery(nameof(FindManyByAscAsync), result);

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

            LogQuery(nameof(FindManyByDescAsync), result);

            return result?.ToListAsync();
        }

        public virtual IQueryable<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes)
        {
            var result = DbSet.Where(i => true);

            foreach (var includeExpression in includes)
            {
                result = result.Include(includeExpression);
            }
            LogQuery(nameof(GetAll), result);
            return result;
        }

        public virtual Task<List<TEntity>> GetAllAsync()
        {
            return DbSet.ToListAsync();
        }
        public async Task<List<TEntity>> GetLatestGroupedBy(
            Expression<Func<TEntity, object>> groupBy)
        {
            var idsQuery = DbSet
                .GroupBy(groupBy)
                .Select(p => p.Max(x => x.Id));

            var ids = await idsQuery.ToListAsync();

            var result = DbSet.Where(p => ids.Contains(p.Id));
            LogQuery(nameof(idsQuery), idsQuery);
            LogQuery(nameof(GetLatestGroupedBy), result);
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
            LogQuery(nameof(idsQuery), idsQuery);
            LogQuery(nameof(GetLatestGroupedBy), result);
            return await result?.ToListAsync();
        }

        public virtual Task<List<TEntity>> LastAsync(int last)
        {
            var result = DbSet.OrderByDescending(x => x).Take(last);
            LogQuery(nameof(LastAsync), result);
            return result?.ToListAsync();
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
        static object Unbox(object x, Type t)
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            if (Nullable.GetUnderlyingType(t) != null)
            {
                return Convert.ChangeType(x, underlyingType);
            }
            return Convert.ChangeType(x, t);
        }

        static void LogQuery<T>(string method, IQueryable<T> result)
        {
#if DEBUG
            Log.Information($"{method} : {result?.ToQueryString()}");
#endif
        }
    }
}