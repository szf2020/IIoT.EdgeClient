using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.DataPipeline.SyncTask;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Infrastructure.Dapper;
using IIoT.Edge.TestSimulator.Consumers;
using IIoT.Edge.TestSimulator.Fakes;
using IIoT.Edge.TestSimulator.Scenarios;
using IIoT.Edge.TestSimulator.Services;
using IIoT.Edge.TestSimulator.Tasks;
using IIoT.Edge.TestSimulator.Views;
using IIoT.Edge.Tasks.DataPipeline.Services;
using IIoT.Edge.Tasks.DataPipeline.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace IIoT.Edge.TestSimulator;

public partial class App : Application
{
    private IServiceProvider _sp = null!;
    private CancellationTokenSource _appCts = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _sp = services.BuildServiceProvider();

        await _sp.InitializeDapperTablesAsync();

        // 启动队列消费后台任务（贯穿整个 App 生命周期）
        var processQueue = _sp.GetRequiredService<ProcessQueueTask>();
        _ = processQueue.StartAsync(_appCts.Token);

        var mainWindow = _sp.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appCts.Cancel();
        _appCts.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── 测试 DB 目录（发布目录下 data 文件夹，方便查看） ─────────────
        var testDbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        // ── Fakes（替换外部边界） ───────────────────────────────
        services.AddSingleton<FakeHttpClient>();
        services.AddSingleton<ICloudHttpClient>(sp => sp.GetRequiredService<FakeHttpClient>());

        services.AddSingleton<FakeDeviceService>();
        services.AddSingleton<IDeviceService>(sp => sp.GetRequiredService<FakeDeviceService>());

        services.AddSingleton<FakeLogService>();
        services.AddSingleton<ILogService>(sp => sp.GetRequiredService<FakeLogService>());

        services.AddSingleton<FakeTodayCapacityStore>();
        services.AddSingleton<ITodayCapacityStore>(sp => sp.GetRequiredService<FakeTodayCapacityStore>());

        services.AddSingleton<FakeCapacitySyncTask>();
        services.AddSingleton<ICapacitySyncTask>(sp => sp.GetRequiredService<FakeCapacitySyncTask>());

        services.AddSingleton<FakeDeviceLogSyncTask>();
        services.AddSingleton<IDeviceLogSyncTask>(sp => sp.GetRequiredService<FakeDeviceLogSyncTask>());

        // ── 真实 Dapper Store（测真实落库） ─────────────────────
        services.AddDapperInfrastructure(testDbDir);

        // ── 消费者（Order=10 产能 / Order=30 云端） ──────────────
        services.AddSingleton<SimCapacityConsumer>();
        services.AddSingleton<ICapacityConsumer>(sp => sp.GetRequiredService<SimCapacityConsumer>());
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICapacityConsumer>());

        services.AddSingleton<SimCloudConsumer>();
        services.AddSingleton<ICloudConsumer>(sp => sp.GetRequiredService<SimCloudConsumer>());
        services.AddSingleton<ICellDataConsumer>(sp => sp.GetRequiredService<ICloudConsumer>());

        // ── DataPipeline 核心 ────────────────────────────────────
        services.AddSingleton<DataPipelineService>();
        services.AddSingleton<IDataPipelineService>(sp => sp.GetRequiredService<DataPipelineService>());

        services.AddSingleton<ProcessQueueTask>();

        // ── TestRetryTask（可手动触发的 Cloud 通道重传） ──────────
        services.AddSingleton<TestRetryTask>(sp => new TestRetryTask(
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>(),
            sp.GetRequiredService<IDeviceLogSyncTask>(),
            sp.GetRequiredService<ICapacitySyncTask>()));

        // ── 辅助服务 ────────────────────────────────────────────
        services.AddSingleton<SimDataHelper>();

        // ── 场景 ─────────────────────────────────────────────────
        services.AddSingleton<OnlinePassScenario>();
        services.AddSingleton<OfflineBufferScenario>();
        services.AddSingleton<RetryScenario>();
        services.AddSingleton<RandomPipelineScenario>();
        services.AddSingleton<ScenarioRunner>();

        // ── Views ────────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
