using IIoT.Edge.Common.Domain;
using System.Linq.Expressions;

namespace IIoT.Edge.Common.Repository;

public interface IRepository<T> : IReadRepository<T>
    where T : class, IEntity, IAggregateRoot
{
    T Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除，一条SQL搞定，不走ChangeTracker
    /// </summary>
    Task<int> ExecuteDeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
}