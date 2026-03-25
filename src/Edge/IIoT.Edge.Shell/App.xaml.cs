// 路径：src/Edge/IIoT.Edge.Shell/App.xaml.cs
using IIoT.Edge.Infrastructure;
using IIoT.Edge.Infrastructure.Dapper;
using IIoT.Edge.Module.Hardware.Plc;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Tasks.Context;
using IIoT.Edge.Tasks.DataPipeline;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace IIoT.Edge.Shell
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        private CancellationTokenSource _appCts = new();

        public App()
        {
            _ = typeof(MaterialDesignThemes.Wpf.BundledTheme).Assembly;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // 1. DI 注册
            var services = ConfigureServices(configuration);

            // 2. 构建容器
            ServiceProvider = services.BuildServiceProvider();

            // 3. 初始化数据库和运行时状态
            InitializeInfrastructure();

            // 4. 启动主窗体
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // 5. 启动后台服务
            StartBackgroundServices();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var contextStore = ServiceProvider.GetRequiredService<ProductionContextStore>();
            contextStore.SaveToFile();

            _appCts.Cancel();

            var plcManager = ServiceProvider.GetRequiredService<PlcConnectionManager>();
            plcManager.Dispose();

            base.OnExit(e);
        }

        /// <summary>
        /// 配置所有 DI 注册
        /// </summary>
        private ServiceCollection ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            var viewRegistry = new ViewRegistry();

            // 模块扫描
            var machineModule = configuration["MachineModule"];
            IModuleLoader loader = new ModuleLoader(services, viewRegistry);
            loader.LoadFromDirectory(
                AppDomain.CurrentDomain.BaseDirectory, machineModule);

            // 数据存储路径统一配置
            var dbDir = GetDbDirectory();

            // 各层注册（全部走 DependencyInjection 扩展方法）
            services.AddShell(viewRegistry, configuration, dbDir);

            return services;
        }

        /// <summary>
        /// 初始化数据库和运行时状态
        /// </summary>
        private void InitializeInfrastructure()
        {
            // EF Core Migration
            ServiceProvider.ApplyMigrations();

            // Dapper 建表
            _ = ServiceProvider.InitializeDapperTablesAsync();

            // 恢复生产上下文
            var contextStore = ServiceProvider.GetRequiredService<ProductionContextStore>();
            contextStore.LoadFromFile();
        }

        /// <summary>
        /// 启动所有后台服务
        /// </summary>
        private void StartBackgroundServices()
        {
            // 上下文自动保存
            var contextStore = ServiceProvider.GetRequiredService<ProductionContextStore>();
            _ = contextStore.StartAutoSaveAsync(_appCts.Token, intervalSeconds: 30);

            // 设备寻址
            _ = IdentifyDeviceAsync();

            // PLC 任务
            _ = ServiceProvider.InitializePlcTasksAsync(_appCts.Token);

            // 数据管道（队列消费 + 重传）
            _ = ServiceProvider.StartDataPipelineAsync(_appCts.Token);
        }

        /// <summary>
        /// 统一数据库目录（所有 .db 文件集中存放）
        /// </summary>
        private static string GetDbDirectory()
        {
            var dbDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IIoT.Edge", "db");
            Directory.CreateDirectory(dbDir);
            return dbDir;
        }

        private async Task IdentifyDeviceAsync()
        {
            var deviceService = ServiceProvider
                .GetRequiredService<IIoT.Edge.Contracts.Device.IDeviceService>();
            var footerWidget = ServiceProvider
                .GetRequiredService<IIoT.Edge.UI.Shared.Widgets.Footer.FooterWidget>();
            var logService = ServiceProvider
                .GetRequiredService<IIoT.Edge.Contracts.ILogService>();

            logService.Info("正在进行设备寻址...");
            var success = await deviceService.IdentifyAsync();

            if (success && deviceService.CurrentDevice is not null)
            {
                footerWidget.SetDeviceCode(deviceService.CurrentDevice.DeviceCode);
                logService.Info($"设备寻址成功：{deviceService.CurrentDevice.DeviceCode}");
            }
            else
            {
                footerWidget.SetDeviceCode("未识别");
                logService.Warn("设备寻址失败");
            }
        }
    }
}