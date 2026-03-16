using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // 1. 注册核心基础服务
            var viewRegistry = new ViewRegistry();
            services.AddSingleton<IViewRegistry>(viewRegistry);
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<INavigationService, NavigationService>();

            // 2. 扫描插件并执行【双向注入】：逻辑注入容器 + 样式注入资源池
            LoadPluginsAndStyles(services, viewRegistry);

            // 3. 将注册表里的 View 和 ViewModel 放入容器
            foreach (var route in viewRegistry.Routes.Values)
            {
                services.AddTransient(route.ViewType);
                services.AddTransient(route.ViewModelType);
            }

            // 4. 注册主窗体
            services.AddSingleton<MainWindow>();

            // 5. 构建容器
            ServiceProvider = services.BuildServiceProvider();

            // 6. 菜单初始化
            var shellViewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            var sortedMenus = viewRegistry.Menus.OrderBy(m => m.Order).ToList();
            foreach (var menu in sortedMenus)
            {
                shellViewModel.Menus.Add(menu);
            }

            // 7. 启动
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void LoadPluginsAndStyles(IServiceCollection services, IViewRegistry registry)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var moduleFiles = Directory.GetFiles(basePath, "IIoT.Edge.Module.*.dll");

            foreach (var file in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IEdgeModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in moduleTypes)
                    {
                        var module = (IEdgeModule)Activator.CreateInstance(type);
                        if (module == null) continue;

                        // --- 动作 A: 业务逻辑注册 ---
                        module.ConfigureServices(services);
                        module.ConfigureViews(registry);

                        // --- 动作 B: 样式动态注入 (解决你担心的“接入寻找”问题) ---
                        InjectModuleStyles(assembly.GetName().Name);
                    }
                }
                catch (Exception ex)
                {
                    // 实际开发建议记录到本地日志
                    System.Diagnostics.Debug.WriteLine($"加载插件 {file} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 核心魔法：根据程序集名称，自动寻找并合并 ModuleTheme.xaml
        /// </summary>
        private void InjectModuleStyles(string assemblyName)
        {
            // 按照 pack URI 协议拼接插件样式的地址
            // 约定插件样式主入口必须叫 ModuleTheme.xaml
            var resourceUri = new Uri($"/{assemblyName};component/ModuleTheme.xaml", UriKind.Relative);

            try
            {
                // 尝试加载并合并到全局资源字典中
                var dict = new ResourceDictionary { Source = resourceUri };
                Application.Current.Resources.MergedDictionaries.Add(dict);
                System.Diagnostics.Debug.WriteLine($"成功合并插件样式: {assemblyName}");
            }
            catch
            {
                // 如果插件没有 ModuleTheme.xaml，加载会抛错，直接吞掉即可，说明该插件不带私有样式
            }
        }
    }
}