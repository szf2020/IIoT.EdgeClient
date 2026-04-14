using IIoT.Edge.Application.Features.Hardware.HardwareConfigView.Models;
using MediatR;

namespace IIoT.Edge.Application.Features.Hardware.HardwareConfigView;

/// <summary>
/// 硬件配置页面增删改查服务契约。
/// </summary>
public interface IHardwareConfigCrudService
{
    Task<HardwareConfigInitResult> LoadAsync(CancellationToken cancellationToken = default);

    Task<IoMappingPageResult> LoadIoMappingsAsync(
        int networkDeviceId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<NetworkDeviceVm> networkDevices,
        IReadOnlyCollection<SerialDeviceVm> serialDevices,
        int selectedNetworkDeviceId,
        IReadOnlyCollection<IoMappingVm> ioMappings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 硬件配置页面增删改查服务。
/// 负责将界面操作转发到硬件配置查询与保存命令。
/// </summary>
public sealed class HardwareConfigCrudService(ISender sender) : IHardwareConfigCrudService
{
    public Task<HardwareConfigInitResult> LoadAsync(CancellationToken cancellationToken = default)
        => sender.Send(new LoadHardwareConfigQuery(), cancellationToken);

    public Task<IoMappingPageResult> LoadIoMappingsAsync(
        int networkDeviceId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
        => sender.Send(
            new LoadIoMappingsQuery(networkDeviceId, pageIndex, pageSize),
            cancellationToken);

    public Task SaveAsync(
        IReadOnlyCollection<NetworkDeviceVm> networkDevices,
        IReadOnlyCollection<SerialDeviceVm> serialDevices,
        int selectedNetworkDeviceId,
        IReadOnlyCollection<IoMappingVm> ioMappings,
        CancellationToken cancellationToken = default)
        => sender.Send(
            new SaveHardwareConfigCommand(
                networkDevices.ToList(),
                serialDevices.ToList(),
                selectedNetworkDeviceId,
                ioMappings.ToList()),
            cancellationToken);
}
