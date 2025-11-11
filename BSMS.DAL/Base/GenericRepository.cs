using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BSMS.DAL.Base;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbSet<T> _dbSet;
    protected readonly BSMSDbContext _context;

    public GenericRepository(BSMSDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    #region Query Operations

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (filter != null)
            query = query.Where(filter);

        foreach (var include in includes)
            query = query.Include(include);

        if (orderBy != null)
            query = orderBy(query);

        return await query.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        foreach (var include in includes)
            query = query.Include(include);

        var keyName = _context.Model.FindEntityType(typeof(T))!
        .FindPrimaryKey()!
        .Properties
        .Select(x => x.Name)
        .Single();

        var entity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, keyName) == id);

        if (entity != null)
            _context.Entry(entity).State = EntityState.Detached;

        return entity;
    }

    public virtual async Task<T?> GetSingleAsync(
        Expression<Func<T, bool>> filter,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        foreach (var include in includes)
            query = query.Include(include);

        return await query.FirstOrDefaultAsync(filter);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        foreach (var include in includes)
            query = query.Include(include);

        return await query.Where(predicate).ToListAsync();
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (filter != null)
            query = query.Where(filter);

        var totalCount = await query.CountAsync();

        foreach (var include in includes)
            query = query.Include(include);

        if (orderBy != null)
            query = orderBy(query);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    #endregion

    #region Command Operations (Immediate Save)

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public virtual async Task<IEnumerable<T>> CreateRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        foreach (var entity in entities)
            _context.Entry(entity).State = EntityState.Detached;

        return entities;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        var local = _dbSet.Local.FirstOrDefault(e => e.Equals(entity));
        if (local != null)
            _context.Entry(local).State = EntityState.Detached;

        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        _context.Entry(entity).State = EntityState.Detached;

        return entity;
    }

    public virtual async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync();

        foreach (var entity in entities)
            _context.Entry(entity).State = EntityState.Detached;

        return entities;
    }

    public virtual async Task<bool> DeleteAsync(T entity)
    {
        if (entity == null) return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> DeleteRangeAsync(IEnumerable<T> entities)
    {
        if (entities == null || !entities.Any()) return false;

        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> DeleteRangeAsync(Expression<Func<T, bool>> predicate)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync();
        if (!entities.Any()) return false;

        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Unit of Work Pattern (Prepare + Save)

    public virtual void PrepareCreate(T entity)
    {
        _dbSet.Add(entity);
    }

    public virtual void PrepareCreateRange(IEnumerable<T> entities)
    {
        _dbSet.AddRange(entities);
    }

    public virtual void PrepareUpdate(T entity)
    {
        var local = _dbSet.Local.FirstOrDefault(e => e.Equals(entity));
        if (local != null)
            _context.Entry(local).State = EntityState.Detached;

        _dbSet.Update(entity);
    }

    public virtual void PrepareUpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void PrepareDelete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void PrepareDeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public virtual void DetachAll()
    {
        foreach (var entry in _context.ChangeTracker.Entries<T>())
        {
            entry.State = EntityState.Detached;
        }
    }

    #endregion

    #region Transactions

    public virtual async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    #endregion

    #region Raw SQL

    public virtual async Task<IEnumerable<T>> FromSqlRawAsync(string sql, params object[] parameters)
    {
        return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();
    }

    public virtual async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
    {
        return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    #endregion
}