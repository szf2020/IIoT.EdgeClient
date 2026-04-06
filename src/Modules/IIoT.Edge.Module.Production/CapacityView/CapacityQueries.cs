using IIoT.Edge.Contracts.Events;
using MediatR;

namespace IIoT.Edge.Module.Production.CapacityView;

// ── 聚合结果（Handler 计算好，整包传给 ViewModel）

public record CapacityViewResult(
    List<DailyCapacityVm> Rows,
    int PeriodTotal,
    int PeriodOk,
    int PeriodNg,
    string PeriodYield,
    string AvgDaily);

// ── Queries

public record LoadTodayCapacityQuery(
    Guid DeviceId,
    DateTime Now,
    string PlcName)

    : IRequest<CapacityViewResult>;

public record QueryCapacityHistoryQuery(
    Guid DeviceId,
    string QueryMode,
    DateTime QueryDate,
    string PlcName)

    : IRequest<CapacityViewResult>;

// ── Handlers

public class LoadTodayCapacityHandler(CapacityCloudQueryService service)
    : IRequestHandler<LoadTodayCapacityQuery, CapacityViewResult>
{
    public async Task<CapacityViewResult> Handle(
        LoadTodayCapacityQuery request, CancellationToken ct)
    {
        var date = service.GetProductionDate(request.Now);
        var rows = await service.QueryByProductionDayAsync(
            request.DeviceId, date, request.PlcName);

        return CapacityQueryHelper.ToResult(rows, divisor: 1);
    }
}

public class QueryCapacityHistoryHandler(CapacityCloudQueryService service)
    : IRequestHandler<QueryCapacityHistoryQuery, CapacityViewResult>
{
    public async Task<CapacityViewResult> Handle(
        QueryCapacityHistoryQuery request, CancellationToken ct)
    {
        List<DailyCapacityVm> rows = request.QueryMode switch
        {
            "按月查询" => await service.QueryByMonthAsync(
                             request.DeviceId, request.QueryDate.Year,

                             request.QueryDate.Month, request.PlcName),

            "按年查询" => await service.QueryByYearAsync(
                             request.DeviceId, request.QueryDate.Year, request.PlcName),

            _ => await service.QueryByProductionDayAsync(
                             request.DeviceId, request.QueryDate.Date, request.PlcName)

        };
        int divisor = request.QueryMode == "按年查询" ? 12 : Math.Max(1, rows.Count);
        return CapacityQueryHelper.ToResult(rows, divisor);
    }
}

// ── Notification Handler

public class CapacityViewUpdatedHandler(CapacityViewWidget widget)
    : INotificationHandler<CapacityUpdatedNotification>
{
    public Task Handle(CapacityUpdatedNotification notification, CancellationToken ct)
    {
        widget.OnCapacityUpdated();
        return Task.CompletedTask;
    }
}

// ── 内部工具

internal static class CapacityQueryHelper
{
    internal static CapacityViewResult ToResult(List<DailyCapacityVm> rows, int divisor)
    {
        int total = rows.Sum(x => x.Total);
        int ok = rows.Sum(x => x.OkCount);
        int ng = rows.Sum(x => x.NgCount);
        string yld = total > 0 ? $"{ok * 100.0 / total:F2}%" : "0%";
        string avg = $"{total / Math.Max(1, divisor)}";
        return new CapacityViewResult(rows, total, ok, ng, yld, avg);
    }
}