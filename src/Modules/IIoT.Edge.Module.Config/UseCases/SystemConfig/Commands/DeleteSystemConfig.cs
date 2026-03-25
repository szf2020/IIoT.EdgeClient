using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Module.Config.UseCases.SystemConfig.Commands;

/// <summary>
/// 命令：删除单条系统配置
/// </summary>
public record DeleteSystemConfigCommand(int Id) : ICommand<Result>;

public class DeleteSystemConfigHandler(
    IRepository<SystemConfigEntity> repo,
    IEdgeCacheService cache
) : ICommandHandler<DeleteSystemConfigCommand, Result>
{
    private const string CacheKey = "Config:SystemAll";

    public async Task<Result> Handle(
        DeleteSystemConfigCommand request,
        CancellationToken cancellationToken)
    {
        await repo.ExecuteDeleteAsync(x => x.Id == request.Id);
        cache.Remove(CacheKey);
        return Result.Success();
    }
}