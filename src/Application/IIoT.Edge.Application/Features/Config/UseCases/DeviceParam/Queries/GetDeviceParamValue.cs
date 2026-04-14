using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Result;
using MediatR;

namespace IIoT.Edge.Application.Features.Config.UseCases.DeviceParam.Queries;

/// <summary>
/// 查询：根据设备 Id 和参数键获取单个参数值。
/// </summary>
public record GetDeviceParamValueQuery(int DeviceId, DeviceParamKey Key) : IQuery<Result<string?>>;

public class GetDeviceParamValueHandler(
    ISender sender
) : IQueryHandler<GetDeviceParamValueQuery, Result<string?>>
{
    public async Task<Result<string?>> Handle(
        GetDeviceParamValueQuery request,
        CancellationToken cancellationToken)
    {
        var allResult = await sender.Send(
            new GetDeviceParamsQuery(request.DeviceId), cancellationToken);

        if (!allResult.IsSuccess)
            return Result.Failure("获取设备参数列表失败");

        var value = allResult.Value?
            .FirstOrDefault(x => x.Name == request.Key.ToString())?.Value;

        return Result.Success(value);
    }
}
