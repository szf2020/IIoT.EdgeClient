using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Tasks.DataPipeline.Consumers;
using IIoT.Edge.Tasks.DataPipeline.Services;
using IIoT.Edge.Tasks.DataPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Tasks.DataPipeline;

public static class DependencyInjection
{
    public static IServiceCollection AddDataPipeline(this IServiceCollection services)
    {
        // 入队服务
        services.AddSingleton<DataPipelineService>();
        services.AddSingleton<IDataPipelineService>(sp => sp.GetRequiredService<DataPipelineService>());

        // ── 消费者桥接到消费链 ──────────────────────────────────

        // Order=10 产能统计（实现在 CloudSync 层注册）
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICapacityConsumer>());

        // Order=20 MES（预留，后期注册）
        // services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IMesConsumer>());

        // Order=30 云端上报（实现在 CloudSync 层注册）
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICloudConsumer>());

        // Order=40 Excel：由 AddExcelInfrastructure 统一注册，此处不重复注册，否则每条记录写入两次

        // Order=50 UI 通知
        services.AddSingleton<IUiNotifyConsumer, UiNotifyConsumer>();
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IUiNotifyConsumer>());

        // ── 后台任务 ────────────────────────────────────────────

        // 主队列消费
        services.AddSingleton<ProcessQueueTask>();

        // Cloud 通道补传（含生产数据 + 日志 + 产能）
        services.AddSingleton<RetryTask>(sp => new RetryTask(
            "Cloud",
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>(),
            sp.GetRequiredService<IDeviceLogSyncTask>(),
            sp.GetRequiredService<ICapacitySyncTask>()));

        // MES 通道补传（仅生产数据）
        services.AddSingleton<RetryTask>(sp => new RetryTask(
            "MES",
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>()));

        return services;
    }

    public static Task StartDataPipelineAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var tasks = new List<Task>();

        var processQueue = serviceProvider.GetRequiredService<ProcessQueueTask>();
        tasks.Add(Task.Factory.StartNew(
            () => processQueue.StartAsync(ct),
            ct, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap());

        var retryTasks = serviceProvider.GetServices<RetryTask>();
        foreach (var retryTask in retryTasks)
        {
            tasks.Add(Task.Factory.StartNew(
                () => retryTask.StartAsync(ct),
                ct, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap());
        }

        return Task.WhenAll(tasks);
    }
}