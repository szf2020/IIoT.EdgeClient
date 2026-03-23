// 路径：src/Edge/IIoT.Edge.Shell/App.xaml.cs
using IIoT.Edge.CloudSync.Auth;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.CloudSync.Device;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.Widgets.Footer;
using IIoT.Edge.UI.Shared.Widgets.Login;
using IIoT.Edge.UI.Shared.Widgets.SysMenu;
using IIoT.Edge.UI.Shared.Widgets.SystemHeader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            var viewRegistry = new ViewRegistry();

            // 1. 模块扫描
            var machineModule = configuration["MachineModule"];
            IModuleLoader loader = new ModuleLoader(services, viewRegistry);
            loader.LoadFromDirectory(AppDomain.CurrentDomain.BaseDirectory, machineModule);

            // 2. Shell 核心服务
            services.AddSingleton<IViewRegistry>(viewRegistry);
            services.AddSingleton<INavigationService, NavigationService>();

            // 3. 本地管理员配置
            var localAdminConfig = new LocalAdminConfig();
            configuration.GetSection("LocalAdmin").Bind(localAdminConfig);
            services.AddSingleton(localAdminConfig);

            // 4. HttpClient + AuthService（10秒）
            var baseUrl = configuration["CloudApi:BaseUrl"] ?? "http://10.98.90.154:81";
            services.AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<AuthService>());

            // 5. HttpClient + DeviceService（3秒）
            services.AddHttpClient<DeviceService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(3);
            });
            services.AddSingleton<IDeviceService>(sp => sp.GetRequiredService<DeviceService>());

            // 6. 系统级 Widget
            services.AddSingleton<HeaderWidget>();
            services.AddSingleton<SysMenuWidget>();
            services.AddSingleton<LoginWidget>();
            services.AddSingleton<FooterWidget>();

            // 7. 主窗体
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            // 8. 构建容器
            ServiceProvider = services.BuildServiceProvider();

            // 9. 启动主窗体
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // 10. 异步设备寻址（不阻塞UI启动）
            _ = IdentifyDeviceAsync();
        }

        private async Task IdentifyDeviceAsync()
        {
            var deviceService = ServiceProvider.GetRequiredService<IDeviceService>();
            var footerWidget = ServiceProvider.GetRequiredService<FooterWidget>();
            var logService = ServiceProvider.GetRequiredService<ILogService>();

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
                logService.Warn("设备寻址失败，请检查网络或云端设备注册");
            }
        }
    }
}