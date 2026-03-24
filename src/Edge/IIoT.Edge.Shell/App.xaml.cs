using IIoT.Edge.CloudSync;
using IIoT.Edge.Infrastructure;
using IIoT.Edge.PlcDevice;
using IIoT.Edge.Module.Hardware;
using IIoT.Edge.Module.Production;
using IIoT.Edge.Module.Config;
using IIoT.Edge.Module.Formula;
using IIoT.Edge.Module.SysLog;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.UI.Shared;
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

            var services = new ServiceCollection();
            var viewRegistry = new ViewRegistry();

            // 1. 模块扫描（只做路由/菜单注册，不注册服务）
            var machineModule = configuration["MachineModule"];
            IModuleLoader loader = new ModuleLoader(services, viewRegistry);
            loader.LoadFromDirectory(
                AppDomain.CurrentDomain.BaseDirectory, machineModule);

            // 2. 各层 DI — 每层自己的扩展方法，App只调不写
            var dbPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "IIoT.Edge", "edge.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            services.AddInfrastructure(dbPath);
            services.AddCloudSync(configuration);
            services.AddPlcDevice();
            services.AddShellWidgets();
            services.AddShell(viewRegistry);
            services.AddHardwareModule();
            services.AddProductionModule();
            services.AddConfigModule();
            services.AddFormulaModule();
            services.AddSysLogModule();

            // 已删除: services.AddAutoMapper(...)  → 移入 Shell DI
            // 已删除: services.AddSingleton<IViewRegistry>(...)  → 移入 Shell DI
            // 已删除: services.AddSingleton<INavigationService>(...)  → 移入 Shell DI

            // 3. 构建容器
            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.ApplyMigrations();

            // 4. 启动主窗体
            var mainWindow = ServiceProvider
                .GetRequiredService<MainWindow>();
            mainWindow.Show();

            // 5. 异步设备寻址
            _ = IdentifyDeviceAsync();
        }

        private async Task IdentifyDeviceAsync()
        {
            var deviceService = ServiceProvider
                .GetRequiredService<
                    IIoT.Edge.Contracts.Device.IDeviceService>();
            var footerWidget = ServiceProvider
                .GetRequiredService<
                    IIoT.Edge.UI.Shared.Widgets.Footer.FooterWidget>();
            var logService = ServiceProvider
                .GetRequiredService<
                    IIoT.Edge.Contracts.ILogService>();

            logService.Info("正在进行设备寻址...");
            var success = await deviceService.IdentifyAsync();

            if (success
                && deviceService.CurrentDevice is not null)
            {
                footerWidget.SetDeviceCode(
                    deviceService.CurrentDevice.DeviceCode);
                logService.Info(
                    $"设备寻址成功：{deviceService.CurrentDevice.DeviceCode}");
            }
            else
            {
                footerWidget.SetDeviceCode("未识别");
                logService.Warn("设备寻址失败");
            }
        }
    }
}