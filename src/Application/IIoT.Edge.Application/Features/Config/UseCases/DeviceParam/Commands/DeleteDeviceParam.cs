using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Application.Abstractions.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Application.Features.Config.UseCases.DeviceParam.Commands;

/// <summary>
/// 命令：删除单条设备参数。
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

