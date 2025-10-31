using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace BSMS.DAL.Base;

public interface IGenericRepository<T> where T : class
{
    #region Query Operations

    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);

    Task<T?> GetSingleAsync(
        Expression<Func<T, bool>> filter,
        params Expression<Func<T, object>>[] includes);

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

    #endregion

    #region Command Operations (Immediate Save)

    Task<T> CreateAsync(T entity);

    Task<IEnumerable<T>> CreateRangeAsync(IEnumerable<T> entities);

    Task<T> UpdateAsync(T entity);

    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

    Task<bool> DeleteAsync(T entity);

    Task<bool> DeleteAsync(int id);

    Task<bool> DeleteRangeAsync(IEnumerable<T> entities);

    Task<bool> DeleteRangeAsync(Expression<Func<T, bool>> predicate);

    #endregion

    #region Unit of Work Pattern (Prepare + Save)

    void PrepareCreate(T entity);

    void PrepareCreateRange(IEnumerable<T> entities);

    void PrepareUpdate(T entity);

    void PrepareUpdateRange(IEnumerable<T> entities);

    void PrepareDelete(T entity);

    void PrepareDeleteRange(IEnumerable<T> entities);

    Task<int> SaveAsync();

    void DetachAll();

    #endregion

    #region Transactions

    Task<IDbContextTransaction> BeginTransactionAsync();

    #endregion

    #region Raw SQL

    Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters);

    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

    #endregion
}