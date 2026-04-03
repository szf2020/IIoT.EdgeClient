// 新增文件
// 路径：src/Modules/IIoT.Edge.Module.Hardware/HardwareConfigView/HardwareConfigQueries.cs
//
// Query 层：Handler 禁止操作 UI，只返回数据。
// NetworkDeviceVm / SerialDeviceVm / IoMappingVm 来自已有的 Models/ 子目录，不重复定义。
// Hardware 程序集在 Shell AddMediatR 的扫描范围内，Handler 自动注册，无需手动注册。
//
// 所有字段名、DTO 构造参数顺序均严格对照原 HardwareConfigWidget.cs SaveAsync() 方法。

using AutoMapper;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Module.Hardware.HardwareConfigView.Models;
using IIoT.Edge.Module.Hardware.UseCases.IoMapping.Commands;
using IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Commands;
using IIoT.Edge.Module.Hardware.UseCases.SerialDevice.Commands;
using MediatR;

namespace IIoT.Edge.Module.Hardware.HardwareConfigView;

// ── 辅助传输结构 ──────────────────────────────────────────────────────────────

/// <summary>初始化加载结果（网络设备列表 + 串口设备列表）</summary>
public record HardwareConfigInitResult(
    List<NetworkDeviceVm> NetworkDevices,
    List<SerialDeviceVm>  SerialDevices);

/// <summary>IO 映射分页结果</summary>
public record IoMappingPageResult(
    List<IoMappingVm> Items,
    int               TotalCount);

// ── Queries ──────────────────────────────────────────────────────────────────

/// <summary>激活时加载所有网络设备 + 串口设备</summary>
public record LoadHardwareConfigQuery : IRequest<HardwareConfigInitResult>;

/// <summary>选中网络设备后分页加载 IO 映射</summary>
public record LoadIoMappingsQuery(int NetworkDeviceId, int PageIndex, int PageSize)
    : IRequest<IoMappingPageResult>;

// ── Commands ─────────────────────────────────────────────────────────────────

/// <summary>保存网络设备 + 串口设备 + IO 映射</summary>
public record SaveHardwareConfigCommand(
    List<NetworkDeviceVm> NetworkDevices,
    List<SerialDeviceVm>  SerialDevices,
    int                   SelectedNetworkDeviceId,
    List<IoMappingVm>     IoMappings)
    : IRequest;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class LoadHardwareConfigHandler(ISender sender, IMapper mapper)
    : IRequestHandler<LoadHardwareConfigQuery, HardwareConfigInitResult>
{
    public async Task<HardwareConfigInitResult> Handle(
        LoadHardwareConfigQuery request, CancellationToken ct)
    {
        var networkResult = await sender.Send(new GetAllNetworkDevicesQuery(), ct);
        var networks      = new List<NetworkDeviceVm>();
        if (networkResult.IsSuccess && networkResult.Value != null)
            foreach (var n in networkResult.Value)
                networks.Add(mapper.Map<NetworkDeviceVm>(n));

        var serialResult = await sender.Send(new GetAllSerialDevicesQuery(), ct);
        var serials      = new List<SerialDeviceVm>();
        if (serialResult.IsSuccess && serialResult.Value != null)
            foreach (var s in serialResult.Value)
                serials.Add(mapper.Map<SerialDeviceVm>(s));

        return new HardwareConfigInitResult(networks, serials);
    }
}

public class LoadIoMappingsHandler(ISender sender, IMapper mapper)
    : IRequestHandler<LoadIoMappingsQuery, IoMappingPageResult>
{
    public async Task<IoMappingPageResult> Handle(
        LoadIoMappingsQuery request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetIoMappingsByDeviceQuery(request.NetworkDeviceId, request.PageIndex, request.PageSize),
            ct);

        if (!result.IsSuccess || result.Value is null)
            return new IoMappingPageResult(new(), 0);

        var items = result.Value.Items
            .Select(item => mapper.Map<IoMappingVm>(item))
            .ToList();

        return new IoMappingPageResult(items, result.Value.TotalCount);
    }
}

public class SaveHardwareConfigHandler(ISender sender)
    : IRequestHandler<SaveHardwareConfigCommand>
{
    public async Task Handle(SaveHardwareConfigCommand request, CancellationToken ct)
    {
        // 网络设备 — 参数顺序与原 SaveAsync 完全一致
        var networkDtos = request.NetworkDevices
            .Select(vm => new NetworkDeviceDto(
                vm.Id, vm.DeviceName, vm.DeviceType,
                vm.DeviceModel, vm.IpAddress, vm.Port1, vm.IsEnabled))
            .ToList();
        await sender.Send(new SaveNetworkDevicesCommand(networkDtos), ct);

        // 串口设备 — 参数顺序与原 SaveAsync 完全一致
        var serialDtos = request.SerialDevices
            .Select(vm => new SerialDeviceDto(
                vm.Id, vm.DeviceName, vm.DeviceType,
                vm.PortName, vm.BaudRate, vm.IsEnabled))
            .ToList();
        await sender.Send(new SaveSerialDevicesCommand(serialDtos), ct);

        // IO 映射 — 只有选中了网络设备才保存，参数顺序与原 SaveAsync 完全一致
        if (request.SelectedNetworkDeviceId != 0)
        {
            var ioDtos = request.IoMappings
                .Select(vm => new IoMappingDto(
                    vm.Id, request.SelectedNetworkDeviceId,
                    vm.Label, vm.PlcAddress,
                    vm.AddressCount, vm.DataType,
                    vm.Direction, vm.SortOrder))
                .ToList();
            await sender.Send(
                new SaveIoMappingsCommand(request.SelectedNetworkDeviceId, ioDtos), ct);
        }
    }
}
