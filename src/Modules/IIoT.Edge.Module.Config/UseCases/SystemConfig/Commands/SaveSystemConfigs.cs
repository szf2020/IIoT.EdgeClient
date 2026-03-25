using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Contracts.Cache;
using IIoT.Edge.Domain.Config.Aggregates;

namespace IIoT.Edge.Module.Config.UseCases.SystemConfig.Commands;

/// <summary>
/// 单条配置的传输结构
/// </summary>
public record SystemConfigDto(
    string Key,
    string Value,
    string? Description = null
);

/// <summary>
/// 命令：保存系统配置（全量覆盖）
/// </summary>
public record SaveSystemConfigsCommand(
    List<SystemConfigDto> Configs
) : ICommand<Result>;

public class SaveSystemConfigsHandler(
    IRepository<SystemConfigEntity> repo,
    IEdgeCacheService cache
) : ICommandHandler<SaveSystemConfigsCommand, Result>
{
    private const string CacheKey = "Config:SystemAll";

    public async Task<Result> Handle(
        SaveSystemConfigsCommand request,
        CancellationToken cancellationToken)
    {
        var valid = request.Configs
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .Select(g => g.Last())
            .ToList();

        await repo.ExecuteDeleteAsync(_ => true);

        for (int i = 0; i < valid.Count; i++)
        {
            var c = valid[i];
            repo.Add(new SystemConfigEntity(c.Key, c.Value, c.Description)
            {
                SortOrder = i + 1
            });
        }
        await repo.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKey);
        return Result.Success();
    }
}