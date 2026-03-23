// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/IModuleLoader.cs
namespace IIoT.Edge.UI.Shared.Modularity
{
    public interface IModuleLoader
    {
        void LoadFromDirectory(string directory, string? machineModule = null);
    }
}