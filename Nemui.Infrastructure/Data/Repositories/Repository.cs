using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nemui.Application.Repositories;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.Common.Abstractions;

namespace Nemui.Infrastructure.Data.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public virtual async ValueTask<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet.FindAsync([id], cancellationToken);
        return result;
    }

    public virtual ValueTask<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        new(_dbSet.FirstOrDefaultAsync(predicate, cancellationToken));

    public virtual async ValueTask<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.ToListAsync(cancellationToken);

    public virtual async ValueTask<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public virtual ValueTask<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        new(_dbSet.AnyAsync(predicate, cancellationToken));

    public virtual ValueTask<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate == null 
            ? new ValueTask<int>(_dbSet.CountAsync(cancellationToken))
            : new ValueTask<int>(_dbSet.CountAsync(predicate, cancellationToken));

    public virtual async ValueTask<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async ValueTask<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<T> ?? entities.ToList();
        await _dbSet.AddRangeAsync(entityList, cancellationToken);
        return entityList;
    }

    public virtual ValueTask UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask<IEnumerable<T>> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities as List<T> ?? entities.ToList();
        _dbSet.AddRange(entityList);
        return ValueTask.FromResult<IEnumerable<T>>(entityList);
    }

    public virtual ValueTask BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask BulkDeleteAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return ValueTask.CompletedTask;
    }

    protected virtual IQueryable<T> ApplyFilters(IQueryable<T> query, Expression<Func<T, bool>>[]? filters)
    {
        if (filters == null || filters.Length == 0) return query;
        return filters.Aggregate(query, (current, filter) => current.Where(filter));
    }

    protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includes)
    {
        return includes.Aggregate(query, (current, include) => current.Include(include));
    }
}