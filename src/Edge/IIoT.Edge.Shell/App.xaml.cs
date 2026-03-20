// 路径：src/Edge/IIoT.Edge.Shell/App.xaml.cs
using IIoT.Edge.CloudSync.Auth;
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared.Modularity;
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
            IModuleLoader loader = new ModuleLoader(services, viewRegistry);
            loader.LoadFromDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // 2. Shell 核心服务
            services.AddSingleton<IViewRegistry>(viewRegistry);
            services.AddSingleton<INavigationService, NavigationService>();

            // 本地管理员配置
            var localAdminConfig = new LocalAdminConfig();
            configuration.GetSection("LocalAdmin").Bind(localAdminConfig);
            services.AddSingleton(localAdminConfig);

            // HttpClient + AuthService（CloudSync 实现）
            var baseUrl = configuration["CloudApi:BaseUrl"] ?? "http://10.98.90.154:81";
            services.AddHttpClient<AuthService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<AuthService>());

            // 5. 系统级 Widget
            services.AddSingleton<HeaderWidget>();
            services.AddSingleton<SysMenuWidget>();
            services.AddSingleton<LoginWidget>();
            // 6. 主窗体
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            // 7. 构建容器并启动
            ServiceProvider = services.BuildServiceProvider();
            ServiceProvider.GetRequiredService<MainWindow>().Show();
        }
    }
}