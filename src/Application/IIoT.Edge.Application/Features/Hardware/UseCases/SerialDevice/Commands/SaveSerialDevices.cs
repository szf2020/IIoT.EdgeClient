using IIoT.Edge.SharedKernel.Messaging;
using IIoT.Edge.SharedKernel.Repository;
using IIoT.Edge.SharedKernel.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Application.Features.Hardware.UseCases.SerialDevice.Commands;

/// <summary>
/// 单条串口设备的数据传输对象。
/// </summary>
public record SerialDeviceDto(
    int Id,
    string DeviceName,
    string DeviceType,
    string PortName,
    int BaudRate,
    bool IsEnabled
);

/// <summary>
/// 命令：保存串口设备列表，按提交结果进行新增或更新。
/// </summary>
public record SaveSerialDevicesCommand(
    List<SerialDeviceDto> Devices
) : ICommand<Result>;

/// <summary>
/// 处理器：保存串口设备配置。
/// </summary>
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
