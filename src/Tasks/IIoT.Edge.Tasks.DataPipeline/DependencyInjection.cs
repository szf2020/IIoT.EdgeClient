using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Tasks.DataPipeline.Services;
using IIoT.Edge.Tasks.DataPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Tasks.DataPipeline;

public static class DependencyInjection
{
    /// <summary>
    /// 注册数据管道服务
    /// 
    /// 在 App.xaml.cs 中调用：
    ///   services.AddDataPipeline();
    /// </summary>
    public static IServiceCollection AddDataPipeline(this IServiceCollection services)
    {
        // 入队服务（单例，内部持有 ConcurrentQueue）
        services.AddSingleton<DataPipelineService>();
        services.AddSingleton<IDataPipelineService>(sp => sp.GetRequiredService<DataPipelineService>());

        // 两个后台任务（单例）
        services.AddSingleton<ProcessQueueTask>();
        services.AddSingleton<RetryTask>();

        return services;
    }

    /// <summary>
    /// 容器构建后调用：启动队列消费和重传任务
    /// 
    /// 在 App.xaml.cs 中调用：
    ///   await ServiceProvider.StartDataPipelineAsync(appCts.Token);
    /// </summary>
    public static async Task StartDataPipelineAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var processQueue = serviceProvider.GetRequiredService<ProcessQueueTask>();
        var retryTask = serviceProvider.GetRequiredService<RetryTask>();

        // 后台启动，不阻塞
        _ = Task.Run(() => processQueue.StartAsync(ct), ct);
        _ = Task.Run(() => retryTask.StartAsync(ct), ct);
    }
}