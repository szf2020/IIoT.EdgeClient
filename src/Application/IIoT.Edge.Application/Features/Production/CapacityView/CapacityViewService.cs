using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.Device;
using MediatR;

namespace IIoT.Edge.Application.Features.Production.CapacityView;

/// <summary>
/// 产能界面服务契约。
/// 向 Presentation 层提供设备列表、联网状态和产能查询能力。
/// </summary>
public interface ICapacityViewService
{
    event Action<NetworkState>? NetworkStateChanged;

    bool IsOnline { get; }

    IReadOnlyList<string> GetDeviceNames();

    Task<CapacityViewResult> LoadTodayAsync(string plcName, CancellationToken cancellationToken = default);

    Task<CapacityViewResult> QueryHistoryAsync(string queryMode, DateTime queryDate, string plcName, CancellationToken cancellationToken = default);
}

/// <summary>
/// 产能界面服务。
/// 负责衔接设备上下文、联网状态与产能查询用例。
/// </summary>
public sealed class CapacityViewService(
    ISender sender,
    IProductionContextStore contextStore,
    IDeviceService deviceService) : ICapacityViewService
{
    public event Action<NetworkState>? NetworkStateChanged
    {
        add => deviceService.NetworkStateChanged += value;
        remove => deviceService.NetworkStateChanged -= value;
    }

    public bool IsOnline => deviceService.CurrentState == NetworkState.Online;

    public IReadOnlyList<string> GetDeviceNames()
        => contextStore.GetAll()
            .Select(context => context.DeviceName)
            .OrderBy(name => name)
            .ToList();

    public async Task<CapacityViewResult> LoadTodayAsync(string plcName, CancellationToken cancellationToken = default)
    {
        var deviceId = deviceService.CurrentDevice?.DeviceId;
        if (deviceId is null)
            return new CapacityViewResult(new(), 0, 0, 0, "0%", "0");

        return await sender.Send(
            new LoadTodayCapacityQuery(deviceId.Value, DateTime.Now, plcName),
            cancellationToken);
    }

    public async Task<CapacityViewResult> QueryHistoryAsync(
        string queryMode,
        DateTime queryDate,
        string plcName,
        CancellationToken cancellationToken = default)
    {
        var deviceId = deviceService.CurrentDevice?.DeviceId;
        if (deviceId is null)
            return new CapacityViewResult(new(), 0, 0, 0, "0%", "0");

        return await sender.Send(
            new QueryCapacityHistoryQuery(deviceId.Value, queryMode, queryDate, plcName),
            cancellationToken);
    }
}
