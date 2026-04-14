using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Consumers;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.DataPipeline.SyncTask;
using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Runtime.DataPipeline.Consumers;
using IIoT.Edge.Runtime.DataPipeline.Services;
using IIoT.Edge.Runtime.DataPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Runtime.DataPipeline;

public static class DependencyInjection
{
    public static IServiceCollection AddEdgeDataPipelineRuntime(this IServiceCollection services)
    {
        services.AddSingleton<DataPipelineService>();
        services.AddSingleton<IDataPipelineService>(sp => sp.GetRequiredService<DataPipelineService>());

        // 顺序 10：产能统计消费者（在 CloudSync 中注册具体实现）
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICapacityConsumer>());

        // 顺序 20：MES 消费者（预留）
        // services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IMesConsumer>());

        // 顺序 30：云端上报消费者（在 CloudSync 中注册具体实现）
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICloudConsumer>());

        // 顺序 50：界面通知消费者
        services.AddSingleton<IUiNotifyConsumer, UiNotifyConsumer>();
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IUiNotifyConsumer>());

        services.AddSingleton<ProcessQueueTask>();

        // Cloud 通道重试：处理电芯数据、设备日志和产能缓冲
        services.AddSingleton<RetryTask>(sp => new RetryTask(
            "Cloud",
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>(),
            sp.GetRequiredService<IDeviceLogSyncTask>(),
            sp.GetRequiredService<ICapacitySyncTask>(),
            sp.GetService<ICloudBatchConsumer>()));

        // MES 通道重试：仅处理电芯数据
        services.AddSingleton<RetryTask>(sp => new RetryTask(
            "MES",
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>()));

        return services;
    }

    public static Task StartEdgeDataPipelineRuntimeAsync(
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
