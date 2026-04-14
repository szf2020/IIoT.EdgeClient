using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Result;
using MediatR;

namespace IIoT.Edge.Application.Features.Config.UseCases.SystemConfig.Queries;

/// <summary>
/// 查询：根据 Key 获取单个系统配置值。
/// </summary>
public record GetSystemConfigValueQuery(SystemConfigKey Key) : IQuery<Result<string?>>;

public class GetSystemConfigValueHandler(
    ISender sender
) : IQueryHandler<GetSystemConfigValueQuery, Result<string?>>
{
    public async Task<Result<string?>> Handle(
        GetSystemConfigValueQuery request,
        CancellationToken cancellationToken)
    {
        // 复用 GetAllSystemConfigs，从缓存读取，避免重复查询数据库
        var allResult = await sender.Send(new GetAllSystemConfigsQuery(), cancellationToken);

        if (!allResult.IsSuccess)
            return Result.Failure("获取配置列表失败");

        var value = allResult.Value?
            .FirstOrDefault(x => x.Key == request.Key.ToString())?.Value;

        return Result.Success(value);
    }
}
