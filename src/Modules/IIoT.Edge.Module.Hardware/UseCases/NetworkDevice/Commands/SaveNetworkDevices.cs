using IIoT.Edge.Common.Enums;
using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Commands;

public record NetworkDeviceDto(
    int Id,
    string DeviceName,
    DeviceType DeviceType,
    string? DeviceModel,
    string IpAddress,
    int Port1,
    bool IsEnabled
);

public record SaveNetworkDevicesCommand(
    List<NetworkDeviceDto> Devices
) : ICommand<Result>;

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