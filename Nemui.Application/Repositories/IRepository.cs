using System.Linq.Expressions;
using Nemui.Shared.Common.Abstractions;

namespace Nemui.Application.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    ValueTask<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    ValueTask<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    ValueTask<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    ValueTask UpdateAsync(T entity, CancellationToken cancellationToken = default);
    ValueTask UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(T entity, CancellationToken cancellationToken = default);
    ValueTask DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<T>> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    ValueTask BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    ValueTask BulkDeleteAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
}