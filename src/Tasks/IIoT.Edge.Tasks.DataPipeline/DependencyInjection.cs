using IIoT.Edge.Contracts.DataPipeline;
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

        // UI 通知 + 产能消费者
        services.AddSingleton<IUiNotifyConsumer, UiNotifyConsumer>();
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<IUiNotifyConsumer>());

        // 后台任务
        services.AddSingleton<ProcessQueueTask>();
        services.AddSingleton<RetryTask>();

        return services;
    }

    public static async Task StartDataPipelineAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var processQueue = serviceProvider.GetRequiredService<ProcessQueueTask>();
        var retryTask = serviceProvider.GetRequiredService<RetryTask>();

        _ = Task.Run(() => processQueue.StartAsync(ct), ct);
        _ = Task.Run(() => retryTask.StartAsync(ct), ct);
    }
}