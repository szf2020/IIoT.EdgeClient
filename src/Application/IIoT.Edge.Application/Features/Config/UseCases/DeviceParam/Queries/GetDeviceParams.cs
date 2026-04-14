using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Application.Abstractions.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Application.Features.Config.UseCases.DeviceParam.Queries;

/// <summary>
/// 查询：获取指定设备的全部参数。
/// </summary>
public record GetDeviceParamsQuery(int DeviceId) : IQuery<Result<List<DeviceParamEntity>>>;

public class GetDeviceParamsHandler(
    IReadRepository<DeviceParamEntity> repo,
    IEdgeCacheService cache
) : IQueryHandler<GetDeviceParamsQuery, Result<List<DeviceParamEntity>>>
{
    private const string CachePrefix = "Config:DeviceParam:";

    public async Task<Result<List<DeviceParamEntity>>> Handle(
        GetDeviceParamsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CachePrefix + request.DeviceId;

        var cached = cache.Get<List<DeviceParamEntity>>(cacheKey);
        if (cached != null)
            return Result.Success(cached);

        var list = await repo.GetListAsync(
            x => x.NetworkDeviceId == request.DeviceId, cancellationToken);
        cache.Set(cacheKey, list);
        return Result.Success(list);
    }
}
