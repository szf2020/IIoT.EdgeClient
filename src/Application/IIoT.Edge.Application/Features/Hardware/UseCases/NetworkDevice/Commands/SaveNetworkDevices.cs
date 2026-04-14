using IIoT.Edge.SharedKernel.Enums;
using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Application.Features.Hardware.UseCases.NetworkDevice.Commands;

/// <summary>
/// 单条网络设备的数据传输对象。
/// </summary>
public record NetworkDeviceDto(
    int Id,
    string DeviceName,
    DeviceType DeviceType,
    string? DeviceModel,
    string IpAddress,
    int Port1,
    bool IsEnabled
);

/// <summary>
/// 命令：保存网络设备列表，按提交结果进行新增或更新。
/// </summary>
public record SaveNetworkDevicesCommand(
    List<NetworkDeviceDto> Devices
) : ICommand<Result>;

/// <summary>
/// 处理器：保存网络设备配置。
/// </summary>
public class SaveNetworkDevicesHandler(
    IRepository<NetworkDeviceEntity> repo
) : ICommandHandler<SaveNetworkDevicesCommand, Result>
{
    public async Task<Result> Handle(
        SaveNetworkDevicesCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var dto in request.Devices)
        {
            if (dto.Id == 0)
            {
                var entity = new NetworkDeviceEntity(
                    dto.DeviceName, dto.DeviceType, dto.IpAddress, dto.Port1)
                {
                    DeviceModel = dto.DeviceModel,
                    IsEnabled = dto.IsEnabled
                };
                repo.Add(entity);
            }
            else
            {
                var entity = await repo.GetByIdAsync(dto.Id, cancellationToken);
                if (entity != null)
                {
                    entity.DeviceName = dto.DeviceName;
                    entity.DeviceType = dto.DeviceType;
                    entity.DeviceModel = dto.DeviceModel;
                    entity.IpAddress = dto.IpAddress;
                    entity.Port1 = dto.Port1;
                    entity.IsEnabled = dto.IsEnabled;
                    repo.Update(entity);
                }
            }
        }

        await repo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
