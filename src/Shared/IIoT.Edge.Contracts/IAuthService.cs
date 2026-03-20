// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/IAuthService.cs

// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/IAuthService.cs
using IIoT.Edge.Contracts.Model;

namespace IIoT.Edge.Contracts
{
    /// <summary>
    /// 鉴权服务契约。
    /// WPF 唯一的权限判断入口，屏蔽"本地紧急管理员/云端JWT"两种登录方式的差异。
    /// </summary>
    public interface IAuthService
    {
        /// <summary>当前登录用户，未登录为 null</summary>
        UserSession? CurrentUser { get; }

        /// <summary>是否已登录</summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 判断当前用户是否拥有指定权限。
        /// 本地紧急管理员永远返回 true。
        /// 未登录永远返回 false。
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// 本地紧急管理员登录（断网保底，不走云端）。
        /// 密码哈希比对本地配置。
        /// </summary>
        Task<AuthResult> LoginLocalAsync(string password);

        /// <summary>
        /// 云端账号登录。
        /// 调用 POST /api/v1/Identity/login，解析 JWT Claims 填充权限列表。
        /// </summary>
        Task<AuthResult> LoginCloudAsync(string employeeNo, string password);

        /// <summary>注销，清空 CurrentUser</summary>
        void Logout();

        /// <summary>登录状态变更时触发（菜单 IsEnabled 监听此事件刷新）</summary>
        event Action<UserSession?> AuthStateChanged;
    }
}