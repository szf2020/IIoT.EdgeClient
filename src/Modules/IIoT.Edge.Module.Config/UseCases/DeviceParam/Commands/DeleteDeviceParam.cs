using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Module.Config.UseCases.DeviceParam.Commands;

/// <summary>
/// 命令：删除单条设备参数
/// </summary>
public record DeleteDeviceParamCommand(int DeviceId, int ParamId) : ICommand<Result>;

public class DeleteDeviceParamHandler(
    IRepository<DeviceParamEntity> repo,
    IEdgeCacheService cache
) : ICommandHandler<DeleteDeviceParamCommand, Result>
{
    private const string CachePrefix = "Config:DeviceParam:";

    public async Task<Result> Handle(
        DeleteDeviceParamCommand request,
        CancellationToken cancellationToken)
    {
        await repo.ExecuteDeleteAsync(x => x.Id == request.ParamId);
        cache.Remove(CachePrefix + request.DeviceId);
        return Result.Success();
    }
}
