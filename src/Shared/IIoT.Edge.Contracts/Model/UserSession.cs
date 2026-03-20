// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/UserSession.cs
namespace IIoT.Edge.Contracts.Model
{
    /// <summary>
    /// 当前登录用户的会话信息。
    /// 由 IAuthService 填充，只读，外部不能修改。
    /// </summary>
    public class UserSession
    {
        /// <summary>显示名（本地管理员固定"紧急管理员"，云端登录显示真实姓名）</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>是否是本地紧急管理员</summary>
        public bool IsLocalAdmin { get; init; }

        /// <summary>
        /// 从 JWT Permission Claims 解析出的权限集合。
        /// 本地管理员此字段为空，HasPermission 直接用 IsLocalAdmin 短路。
        /// </summary>
        public HashSet<string> Permissions { get; init; } = new();

        /// <summary>JWT Token 原始字符串（本地管理员为 null）</summary>
        public string? Token { get; init; }
    }
}