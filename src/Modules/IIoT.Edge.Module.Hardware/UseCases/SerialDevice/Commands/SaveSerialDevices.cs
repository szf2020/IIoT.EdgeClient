using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.SerialDevice.Commands;

public record SerialDeviceDto(
    int Id,
    string DeviceName,
    string DeviceType,
    string PortName,
    int BaudRate,
    bool IsEnabled
);

public record SaveSerialDevicesCommand(
    List<SerialDeviceDto> Devices
) : ICommand<Result>;

public class SaveSerialDevicesHandler(
    IRepository<SerialDeviceEntity> repo
) : ICommandHandler<SaveSerialDevicesCommand, Result>
{
    public async Task<Result> Handle(
        SaveSerialDevicesCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var dto in request.Devices)
        {
            if (dto.Id == 0)
            {
                var entity = new SerialDeviceEntity(
                    dto.DeviceName, dto.DeviceType, dto.PortName, dto.BaudRate)
                {
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
                    entity.PortName = dto.PortName;
                    entity.BaudRate = dto.BaudRate;
                    entity.IsEnabled = dto.IsEnabled;
                    repo.Update(entity);
                }
            }
        }

        await repo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}