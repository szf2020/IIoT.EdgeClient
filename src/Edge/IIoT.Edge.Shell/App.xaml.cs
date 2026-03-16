// 路径：src/Edge/IIoT.Edge.Shell/App.xaml.cs
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.Shell.Core;
using IIoT.Edge.Shell.ViewModels;

namespace IIoT.Edge.Shell
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            // 必须在 XAML 解析前强行引用底座程序集
            _ = typeof(IIoT.Edge.UI.Shared.Layouts.HeaderView).Assembly;
            _ = typeof(MaterialDesignThemes.Wpf.BundledTheme).Assembly;
            _ = typeof(AvalonDock.Themes.MetroTheme).Assembly;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            var viewRegistry = new ViewRegistry();

            // 1. 将加载器接口注册进容器（如果需要后期动态加载）
            // 但在启动阶段，我们先直接创建实例完成初始化
            IModuleLoader loader = new ModuleLoader(services, viewRegistry);

            // 2. 执行扫描
            loader.LoadFromDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // 3. 注册 Shell 自身服务
            services.AddSingleton<IViewRegistry>(viewRegistry);
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();

            // 4. 构建 DI
            ServiceProvider = services.BuildServiceProvider();

            // 5. 显示主窗体
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}