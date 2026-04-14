using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Application.Abstractions.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Application.Features.Config.UseCases.SystemConfig.Commands;

/// <summary>
/// 命令：删除单条系统配置。
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
