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
        services.AddSingleton<DataPipelineService>();
        services.AddSingleton<IDataPipelineService>(sp => sp.GetRequiredService<DataPipelineService>());

        // Order=10 Capacity statistics (registered in CloudSync)
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICapacityConsumer>());

        // Order=20 MES (reserved)
        // services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IMesConsumer>());

        // Order=30 Cloud upload (registered in CloudSync)
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICloudConsumer>());

        // Order=50 UI notifications
        services.AddSingleton<IUiNotifyConsumer, UiNotifyConsumer>();
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IUiNotifyConsumer>());

        services.AddSingleton<ProcessQueueTask>();

        // Cloud channel retry (cell data + device logs + capacity)
        services.AddSingleton<RetryTask>(sp => new RetryTask(
            "Cloud",
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>(),
            sp.GetRequiredService<IDeviceLogSyncTask>(),
            sp.GetRequiredService<ICapacitySyncTask>(),
            sp.GetService<ICloudBatchConsumer>()));

        // MES channel retry (cell data only)
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