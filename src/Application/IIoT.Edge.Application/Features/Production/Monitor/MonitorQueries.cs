using IIoT.Edge.Application.Abstractions.Context;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.SharedKernel.Context;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;
using MediatR;
using System.Data;
using System.Reflection;

namespace IIoT.Edge.Application.Features.Production.Monitor;

public record DeviceMonitorSnapshot(
    string DeviceName,
    int DayShiftOk,
    int DayShiftNg,
    int DayShiftTotal,
    string DayShiftYield,
    int NightShiftOk,
    int NightShiftNg,
    int NightShiftTotal,
    string NightShiftYield,
    int TotalAll,
    int OkAll,
    int NgAll,
    string YieldAll,
    string DeviceDataSummary,
    string StepSummary,
    int CellCount,
    DataTable CellTable,
    string CloudLinkStatus,
    string RetryQueueStatus);

public record GetMonitorSnapshotQuery : IRequest<List<DeviceMonitorSnapshot>>;

public class GetMonitorSnapshotHandler(
    IProductionContextStore contextStore,
    IDeviceService deviceService,
    IFailedRecordStore failedStore,
    IDeviceLogBufferStore deviceLogBufferStore,
    ICapacityBufferStore capacityBufferStore)
    : IRequestHandler<GetMonitorSnapshotQuery, List<DeviceMonitorSnapshot>>
{
    public async Task<List<DeviceMonitorSnapshot>> Handle(GetMonitorSnapshotQuery request, CancellationToken ct)
    {
        var failedCloudCountTask = failedStore.GetCountAsync("Cloud");
        var failedMesCountTask = failedStore.GetCountAsync("MES");
        var deviceLogBufferCountTask = deviceLogBufferStore.GetCountAsync();
        var capacityBufferCountTask = capacityBufferStore.GetCountAsync();

        await Task.WhenAll(
            failedCloudCountTask,
            failedMesCountTask,
            deviceLogBufferCountTask,
            capacityBufferCountTask);

        var failedCloudCount = await failedCloudCountTask;
        var failedMesCount = await failedMesCountTask;
        var deviceLogBufferCount = await deviceLogBufferCountTask;
        var capacityBufferCount = await capacityBufferCountTask;

        var cloudLinkStatus = BuildCloudLinkStatus(
            deviceService.CurrentState,
            deviceService.HasDeviceId,
            deviceService.CurrentDevice);
        var retryQueueStatus = BuildRetryQueueStatus(
            failedCloudCount,
            failedMesCount,
            deviceLogBufferCount,
            capacityBufferCount);

        var result = new List<DeviceMonitorSnapshot>();
        var contexts = contextStore.GetAll().ToList();

        if (contexts.Count == 0)
        {
            result.Add(new DeviceMonitorSnapshot(
                DeviceName: "System",
                DayShiftOk: 0,
                DayShiftNg: 0,
                DayShiftTotal: 0,
                DayShiftYield: "0%",
                NightShiftOk: 0,
                NightShiftNg: 0,
                NightShiftTotal: 0,
                NightShiftYield: "0%",
                TotalAll: 0,
                OkAll: 0,
                NgAll: 0,
                YieldAll: "0%",
                DeviceDataSummary: "No data",
                StepSummary: "No steps",
                CellCount: 0,
                CellTable: new DataTable(),
                CloudLinkStatus: cloudLinkStatus,
                RetryQueueStatus: retryQueueStatus));

            return result;
        }

        foreach (var ctx in contexts)
        {
            var cap = ctx.TodayCapacity;
            var deviceInfo = string.Join("  ",
                ctx.DeviceBag.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
            var stepInfo = string.Join("  ",
                ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));

            result.Add(new DeviceMonitorSnapshot(
                DeviceName: ctx.DeviceName,
                DayShiftOk: cap.DayShift.OkCount,
                DayShiftNg: cap.DayShift.NgCount,
                DayShiftTotal: cap.DayShift.Total,
                DayShiftYield: cap.DayShift.Yield,
                NightShiftOk: cap.NightShift.OkCount,
                NightShiftNg: cap.NightShift.NgCount,
                NightShiftTotal: cap.NightShift.Total,
                NightShiftYield: cap.NightShift.Yield,
                TotalAll: cap.TotalAll,
                OkAll: cap.OkAll,
                NgAll: cap.NgAll,
                YieldAll: cap.YieldAll,
                DeviceDataSummary: string.IsNullOrEmpty(deviceInfo) ? "No data" : deviceInfo,
                StepSummary: string.IsNullOrEmpty(stepInfo) ? "No steps" : stepInfo,
                CellCount: ctx.CurrentCells.Count,
                CellTable: BuildCellTable(ctx),
                CloudLinkStatus: cloudLinkStatus,
                RetryQueueStatus: retryQueueStatus));
        }

        return result;
    }

    private static string BuildCloudLinkStatus(NetworkState state, bool hasDeviceId, DeviceSession? deviceSession)
    {
        if (state == NetworkState.Offline)
        {
            return "Offline: heartbeat unavailable, cloud upload paused.";
        }

        if (!hasDeviceId || deviceSession is null)
        {
            return "Online: waiting for device identification.";
        }

        return $"Online: identified as {deviceSession.DeviceName}.";
    }

    private static string BuildRetryQueueStatus(
        int failedCloudCount,
        int failedMesCount,
        int deviceLogBufferCount,
        int capacityBufferCount)
    {
        var total = failedCloudCount + failedMesCount + deviceLogBufferCount + capacityBufferCount;
        if (total == 0)
        {
            return "Retry queue empty.";
        }

        return $"Pending {total} (CloudFailed={failedCloudCount}, MESFailed={failedMesCount}, LogBuffer={deviceLogBufferCount}, CapacityBuffer={capacityBufferCount}).";
    }

    private static DataTable BuildCellTable(ProductionContext ctx)
    {
        var table = new DataTable();
        if (ctx.CurrentCells.Count == 0)
        {
            return table;
        }

        var firstCell = ctx.CurrentCells.Values.First();
        var properties = firstCell.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(CellDataBase.ProcessType)
                && p.Name != nameof(CellDataBase.DisplayLabel))
            .ToList();

        foreach (var prop in properties)
        {
            table.Columns.Add(prop.Name, typeof(string));
        }

        foreach (var cell in ctx.CurrentCells.Values)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
            {
                row[prop.Name] = FormatValue(prop.GetValue(cell));
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "--",
        DateTime dt => dt.ToString("HH:mm:ss.fff"),
        bool b => b ? "OK" : "NG",
        double d => d.ToString("F3"),
        _ => value?.ToString() ?? "--"
    };
}
