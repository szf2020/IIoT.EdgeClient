using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Shell.Core
{
    public class ModuleLoader : IModuleLoader
    {
        private readonly IServiceCollection _services;
        private readonly IViewRegistry _registry;

        public ModuleLoader(IServiceCollection services, IViewRegistry registry)
        {
            _services = services;
            _registry = registry;
        }

        public void LoadFromDirectory(string directory)
        {
            if (!Directory.Exists(directory)) return;

            // 严格匹配重构后的模块命名规范
            var moduleFiles = Directory.GetFiles(directory, "IIoT.Edge.Module.*.dll");

            foreach (var file in moduleFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IEdgeModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in moduleTypes)
                    {
                        if (Activator.CreateInstance(type) is IEdgeModule module)
                        {
                            // 1. 注册模块业务服务
                            module.ConfigureServices(_services);
                            // 2. 注册模块视图契约 (菜单、路由、AvalonDock面板)
                            module.ConfigureViews(_registry);
                            // 3. 注入模块私有样式
                            InjectModuleStyles(assembly.GetName().Name!);

                            System.Diagnostics.Debug.WriteLine($"[Success] 模块已激活: {assembly.GetName().Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Error] 模块加载异常 {file}: {ex.Message}");
                }
            }
        }

        private void InjectModuleStyles(string assemblyName)
        {
            // 每个模块的样式入口统一为 ModuleTheme.xaml
            var resourceUri = new Uri($"/{assemblyName};component/ModuleTheme.xaml", UriKind.Relative);
            try
            {
                var dict = new ResourceDictionary { Source = resourceUri };
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch { /* 模块不含私有样式则忽略 */ }
        }
    }
}