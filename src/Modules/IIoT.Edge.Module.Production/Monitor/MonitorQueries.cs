// 新增文件
// 路径：src/Modules/IIoT.Edge.Module.Production/Monitor/MonitorQueries.cs
//
// Query 层：Handler 禁止操作 UI，只返回数据快照。
// DeviceTabVm 保持在 MonitorWidget.cs 原位置不动，此处只新增 Query/Handler。
// 该文件在 Production 程序集内，Shell AddMediatR 已扫描，无需额外注册。

using IIoT.Edge.Common.Context;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts.Context;
using MediatR;
using System.Data;
using System.Reflection;

namespace IIoT.Edge.Module.Production.Monitor;

// ── 数据快照（Handler → ViewModel，不含 ObservableCollection）────────────────

public record DeviceMonitorSnapshot(
    string    DeviceName,
    int       DayShiftOk,
    int       DayShiftNg,
    int       DayShiftTotal,
    string    DayShiftYield,
    int       NightShiftOk,
    int       NightShiftNg,
    int       NightShiftTotal,
    string    NightShiftYield,
    int       TotalAll,
    int       OkAll,
    int       NgAll,
    string    YieldAll,
    string    DeviceDataSummary,
    string    StepSummary,
    int       CellCount,
    DataTable CellTable);

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetMonitorSnapshotQuery : IRequest<List<DeviceMonitorSnapshot>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetMonitorSnapshotHandler(IProductionContextStore contextStore)
    : IRequestHandler<GetMonitorSnapshotQuery, List<DeviceMonitorSnapshot>>
{
    public Task<List<DeviceMonitorSnapshot>> Handle(
        GetMonitorSnapshotQuery request, CancellationToken ct)
    {
        var result = new List<DeviceMonitorSnapshot>();

        foreach (var ctx in contextStore.GetAll())
        {
            var cap = ctx.TodayCapacity;

            string deviceInfo = string.Join("  ",
                ctx.DeviceBag.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));

            string stepInfo = string.Join("  ",
                ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));

            result.Add(new DeviceMonitorSnapshot(
                DeviceName:        ctx.DeviceName,
                DayShiftOk:        cap.DayShift.OkCount,
                DayShiftNg:        cap.DayShift.NgCount,
                DayShiftTotal:     cap.DayShift.Total,
                DayShiftYield:     cap.DayShift.Yield,
                NightShiftOk:      cap.NightShift.OkCount,
                NightShiftNg:      cap.NightShift.NgCount,
                NightShiftTotal:   cap.NightShift.Total,
                NightShiftYield:   cap.NightShift.Yield,
                TotalAll:          cap.TotalAll,
                OkAll:             cap.OkAll,
                NgAll:             cap.NgAll,
                YieldAll:          cap.YieldAll,
                DeviceDataSummary: string.IsNullOrEmpty(deviceInfo) ? "暂无数据" : deviceInfo,
                StepSummary:       string.IsNullOrEmpty(stepInfo)   ? "暂无任务" : stepInfo,
                CellCount:         ctx.CurrentCells.Count,
                CellTable:         BuildCellTable(ctx)));
        }

        return Task.FromResult(result);
    }

    private static DataTable BuildCellTable(ProductionContext ctx)
    {
        var table = new DataTable();
        if (ctx.CurrentCells.Count == 0) return table;

        var firstCell  = ctx.CurrentCells.Values.First();
        var properties = firstCell.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(CellDataBase.ProcessType)
                     && p.Name != nameof(CellDataBase.DisplayLabel))
            .ToList();

        foreach (var prop in properties)
            table.Columns.Add(prop.Name, typeof(string));

        foreach (var cell in ctx.CurrentCells.Values)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
                row[prop.Name] = FormatValue(prop.GetValue(cell));
            table.Rows.Add(row);
        }

        return table;
    }

    private static string FormatValue(object? value) => value switch
    {
        null        => "--",
        DateTime dt => dt.ToString("HH:mm:ss.fff"),
        bool b      => b ? "OK" : "NG",
        double d    => d.ToString("F3"),
        _           => value.ToString() ?? "--"
    };
}
