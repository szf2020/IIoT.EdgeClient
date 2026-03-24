using IIoT.Edge.Common.Domain;
using IIoT.Edge.Common.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IIoT.Edge.Infrastructure.Repositories;

public class EfRepository<T>(
    IDbContextFactory<EdgeDbContext> factory)
    : EfReadRepository<T>(factory), IRepository<T>
    where T : class, IEntity, IAggregateRoot
{
    private EdgeDbContext? _currentDb;

    private EdgeDbContext EnsureDb()
        => _currentDb ??= _factory.CreateDbContext();

    public T Add(T entity)
    {
        EnsureDb().Set<T>().Add(entity);
        return entity;
    }

    public void Update(T entity)
        => EnsureDb().Set<T>().Update(entity);

    public void Delete(T entity)
        => EnsureDb().Set<T>().Remove(entity);

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_currentDb is null) return 0;

        try
        {
            return await _currentDb
                .SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _currentDb.Dispose();
            _currentDb = null;
        }
    }

    public async Task<int> ExecuteDeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        // 独立 DbContext，一条 SQL 批量删除
        // 不经过 ChangeTracker，不影响 _currentDb
        using var db = _factory.CreateDbContext();
        return await db.Set<T>()
            .Where(predicate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}