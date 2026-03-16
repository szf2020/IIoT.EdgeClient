using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.UI.Shared.Modularity
{
    /// <summary>
    /// 插件加载引擎接口 - 负责扫描并激活系统模块
    /// </summary>
    public interface IModuleLoader
    {
        /// <summary>
        /// 从指定物理目录扫描并加载符合规范的 IIoT.Edge.Module.*.dll
        /// </summary>
        /// <param name="directory">绝对路径</param>
        void LoadFromDirectory(string directory);
    }
}