// 新增文件
// 路径：src/Modules/IIoT.Edge.Module.Config/ParamView/ParamViewQueries.cs
//
// Query 层：Handler 禁止操作 UI，只返回数据。
// GeneralParamVm / DeviceParamVm / DeviceParamGroupVm 来自已有的 Models/ 子目录，不重复定义。
// Config 程序集在 Shell AddMediatR 的扫描范围内，Handler 自动注册，无需手动注册。

using AutoMapper;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Module.Config.ParamView.Models;
using IIoT.Edge.Module.Config.UseCases.DeviceParam.Commands;
using IIoT.Edge.Module.Config.UseCases.DeviceParam.Queries;
using IIoT.Edge.Module.Config.UseCases.SystemConfig.Commands;
using IIoT.Edge.Module.Config.UseCases.SystemConfig.Queries;
using MediatR;

namespace IIoT.Edge.Module.Config.ParamView;

// ── 辅助传输结构 ──────────────────────────────────────────────────────────────

/// <summary>设备分组头信息（只含 Id 和显示名，不含参数明细，供懒加载用）</summary>
public record DeviceGroupHeader(int DeviceId, string DeviceName);

/// <summary>初始化加载结果</summary>
public record ParamViewInitResult(
    List<GeneralParamVm>    GeneralParams,
    List<DeviceGroupHeader> DeviceGroups);

// ── Queries ──────────────────────────────────────────────────────────────────

/// <summary>激活时加载通用参数 + 设备分组头信息</summary>
public record LoadParamViewQuery : IRequest<ParamViewInitResult>;

/// <summary>选中设备后懒加载该设备的参数明细</summary>
public record LoadDeviceParamsQuery(int DeviceId) : IRequest<List<DeviceParamVm>>;

// ── Commands ─────────────────────────────────────────────────────────────────

/// <summary>保存通用参数 + 当前选中设备的参数</summary>
public record SaveParamViewCommand(
    List<GeneralParamVm> GeneralParams,
    int                  DeviceId,
    List<DeviceParamVm>  DeviceParams)
    : IRequest;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class LoadParamViewHandler(ISender sender, IMapper mapper)
    : IRequestHandler<LoadParamViewQuery, ParamViewInitResult>
{
    public async Task<ParamViewInitResult> Handle(
        LoadParamViewQuery request, CancellationToken ct)
    {
        // 通用参数
        var sysResult = await sender.Send(new GetAllSystemConfigsQuery(), ct);
        var general   = new List<GeneralParamVm>();
        if (sysResult.IsSuccess && sysResult.Value != null)
        {
            foreach (var e in sysResult.Value.OrderBy(x => x.SortOrder))
                general.Add(mapper.Map<GeneralParamVm>(e));
        }

        // 设备分组头信息（只取可用设备，不加载参数明细）
        var devResult = await sender.Send(new GetAllNetworkDevicesQuery(), ct);
        var groups    = new List<DeviceGroupHeader>();
        if (devResult.IsSuccess && devResult.Value != null)
        {
            foreach (var d in devResult.Value.Where(x => x.IsEnabled))
                groups.Add(new DeviceGroupHeader(
                    d.Id,
                    $"{d.DeviceName} ({d.IpAddress})"));
        }

        return new ParamViewInitResult(general, groups);
    }
}

public class LoadDeviceParamsHandler(ISender sender, IMapper mapper)
    : IRequestHandler<LoadDeviceParamsQuery, List<DeviceParamVm>>
{
    public async Task<List<DeviceParamVm>> Handle(
        LoadDeviceParamsQuery request, CancellationToken ct)
    {
        var result = await sender.Send(new GetDeviceParamsQuery(request.DeviceId), ct);
        if (!result.IsSuccess || result.Value is null) return new();

        return result.Value
            .OrderBy(x => x.SortOrder)
            .Select(e => mapper.Map<DeviceParamVm>(e))
            .ToList();
    }
}

public class SaveParamViewHandler(ISender sender)
    : IRequestHandler<SaveParamViewCommand>
{
    public async Task Handle(SaveParamViewCommand request, CancellationToken ct)
    {
        // 通用参数 — 与原 Widget 的 SaveAsync 逻辑完全一致
        var sysConfigs = request.GeneralParams
            .Select(vm => new SystemConfigDto(vm.Name, vm.Value, vm.Description))
            .ToList();
        await sender.Send(new SaveSystemConfigsCommand(sysConfigs), ct);

        // 设备参数 — 与原 Widget 的 SaveAsync 逻辑完全一致
        var deviceParams = request.DeviceParams
            .Select(vm => new DeviceParamDto(vm.Name, vm.Value, vm.Unit, vm.Min, vm.Max))
            .ToList();
        await sender.Send(new SaveDeviceParamsCommand(request.DeviceId, deviceParams), ct);
    }
}
