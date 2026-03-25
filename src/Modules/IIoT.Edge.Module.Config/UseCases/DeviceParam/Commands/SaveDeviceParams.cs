using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Module.Config.UseCases.DeviceParam.Commands;

/// <summary>
/// 单条设备参数的传输结构
/// </summary>
public record DeviceParamDto(
    string Name,
    string Value,
    string? Unit = null,
    string? MinValue = null,
    string? MaxValue = null
);

/// <summary>
/// 命令：保存设备参数（全量覆盖指定设备的参数）
/// </summary>
public record SaveDeviceParamsCommand(
    int DeviceId,
    List<DeviceParamDto> Params
) : ICommand<Result>;

public class SaveDeviceParamsHandler(
    IRepository<DeviceParamEntity> repo,
    IEdgeCacheService cache
) : ICommandHandler<SaveDeviceParamsCommand, Result>
{
    private const string CachePrefix = "Config:DeviceParam:";

    public async Task<Result> Handle(
        SaveDeviceParamsCommand request,
        CancellationToken cancellationToken)
    {
        var valid = request.Params
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name)
            .Select(g => g.Last())
            .ToList();

        await repo.ExecuteDeleteAsync(x => x.NetworkDeviceId == request.DeviceId);

        for (int i = 0; i < valid.Count; i++)
        {
            var p = valid[i];
            repo.Add(new DeviceParamEntity(
                request.DeviceId, p.Name, p.Value, p.Unit)
            {
                MinValue = p.MinValue,
                MaxValue = p.MaxValue,
                SortOrder = i + 1
            });
        }
        await repo.SaveChangesAsync(cancellationToken);

        cache.Remove(CachePrefix + request.DeviceId);
        return Result.Success();
    }
}