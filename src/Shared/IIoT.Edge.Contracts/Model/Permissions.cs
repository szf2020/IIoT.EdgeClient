// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/Permissions.cs
namespace IIoT.Edge.Contracts.Model
{
    /// <summary>
    /// WPF 菜单权限常量。
    /// 与云端 JWT Permission Claims 字符串保持一致，不能随意改动。
    /// </summary>
    public static class Permissions
    {
        /// <summary>硬件配置页可操作</summary>
        public const string HardwareConfig = "Device.Update";

        /// <summary>参数配置页可操作</summary>
        public const string ParamConfig = "Recipe.Update";

        /// <summary>配方页可见（所有登录用户默认拥有）</summary>
        public const string RecipeRead = "Recipe.Read";
    }
}