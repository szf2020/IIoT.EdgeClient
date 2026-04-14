using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Application.Abstractions.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Application.Features.Config.UseCases.SystemConfig.Queries;

/// <summary>
/// 查询：获取全部系统配置。
/// </summary>
public record GetAllSystemConfigsQuery() : IQuery<Result<List<SystemConfigEntity>>>;

public class GetAllSystemConfigsHandler(
    IReadRepository<SystemConfigEntity> repo,
    IEdgeCacheService cache
) : IQueryHandler<GetAllSystemConfigsQuery, Result<List<SystemConfigEntity>>>
{
    private const string CacheKey = "Config:SystemAll";

    public async Task<Result<List<SystemConfigEntity>>> Handle(
        GetAllSystemConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var cached = cache.Get<List<SystemConfigEntity>>(CacheKey);
        if (cached != null)
            return Result.Success(cached);

        var list = await repo.GetListAsync(_ => true, cancellationToken);
        cache.Set(CacheKey, list);
        return Result.Success(list);
    }
}
