// 路径：src/Edge/IIoT.Edge.Shell/Core/ModuleLoader.cs
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

        // 通用模块前缀（所有机台都加载）
        private static readonly string[] CommonModules =
        {
            "IIoT.Edge.Module.Hardware",
            "IIoT.Edge.Module.Formula",
            "IIoT.Edge.Module.Config",
            "IIoT.Edge.Module.SysLog",
            "IIoT.Edge.Module.Production",
        };

        public ModuleLoader(IServiceCollection services, IViewRegistry registry)
        {
            _services = services;
            _registry = registry;
        }

        public void LoadFromDirectory(string directory, string? machineModule = null)
        {
            if (!Directory.Exists(directory)) return;

            // 1. 加载所有通用模块
            foreach (var moduleName in CommonModules)
            {
                var file = Path.Combine(directory, $"{moduleName}.dll");
                LoadModule(file);
            }

            // 2. 加载机台专属模块（如果配置了的话）
            if (!string.IsNullOrEmpty(machineModule))
            {
                var file = Path.Combine(directory, $"{machineModule}.dll");
                LoadModule(file);
            }
        }

        private void LoadModule(string file)
        {
            if (!File.Exists(file))
            {
                System.Diagnostics.Debug.WriteLine($"[ModuleLoader] 模块文件不存在: {file}");
                return;
            }

            try
            {
                var assembly = Assembly.LoadFrom(file);
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IEdgeModule).IsAssignableFrom(t)
                                && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    if (Activator.CreateInstance(type) is IEdgeModule module)
                    {
                        module.ConfigureServices(_services);
                        module.ConfigureViews(_registry);
                        InjectModuleStyles(assembly.GetName().Name!);

                        System.Diagnostics.Debug.WriteLine(
                            $"[ModuleLoader] 模块已激活: {assembly.GetName().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ModuleLoader] 模块加载异常 {file}: {ex.Message}");
            }
        }

        private void InjectModuleStyles(string assemblyName)
        {
            var resourceUri = new Uri(
                $"/{assemblyName};component/ModuleTheme.xaml", UriKind.Relative);
            try
            {
                var dict = new ResourceDictionary { Source = resourceUri };
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch { /* 模块不含私有样式则忽略 */ }
        }
    }
}