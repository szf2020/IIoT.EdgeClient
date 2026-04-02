using IIoT.Edge.Common.DataPipeline.Capacity;
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
        var testDbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        // ── Fakes ────────────────────────────────────────────────
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

        // ── 真实 Dapper Store ────────────────────────────────────
        services.AddDapperInfrastructure(testDbDir);

        // ── ShiftConfig（历史数据场景需要用到）───────────────────
        services.AddSingleton(new ShiftConfig
        {
            DayStart = "08:30",
            DayEnd = "20:30"
        });

        // ── 消费者 ───────────────────────────────────────────────
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

        // ── TestRetryTask ────────────────────────────────────────
        services.AddSingleton<TestRetryTask>(sp => new TestRetryTask(
            sp.GetRequiredService<ILogService>(),
            sp.GetRequiredService<IFailedRecordStore>(),
            sp.GetRequiredService<IDeviceService>(),
            sp.GetServices<ICellDataConsumer>(),
            sp.GetRequiredService<IDeviceLogSyncTask>(),
            sp.GetRequiredService<ICapacitySyncTask>()));

        // ── 辅助服务 ─────────────────────────────────────────────
        services.AddSingleton<SimDataHelper>();

        // ── 场景 ─────────────────────────────────────────────────
        services.AddSingleton<OnlinePassScenario>();
        services.AddSingleton<OfflineBufferScenario>();
        services.AddSingleton<RetryScenario>();
        services.AddSingleton<HistoricalDataScenario>();  // 新增历史数据场景
        services.AddSingleton<ScenarioRunner>();

        // ── Views ────────────────────────────────────────────────
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }
}