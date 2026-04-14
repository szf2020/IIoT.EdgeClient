using IIoT.Edge.Application.Common.Models;

namespace IIoT.Edge.Application.Abstractions.Auth;

/// <summary>
/// 认证服务契约。
/// 负责维护当前登录状态、权限判断以及本地/云端登录流程。
/// </summary>
public interface IAuthService
{
    UserSession? CurrentUser { get; }
    bool IsAuthenticated { get; }

    bool HasPermission(string permission);

    Task<AuthResult> LoginLocalAsync(string password);

    /// <summary>
    /// 通过云端完成登录，并返回与当前设备绑定的认证结果。
    /// </summary>
    Task<AuthResult> LoginCloudAsync(
        string employeeNo,
        string password,
        Guid deviceId);

    void Logout();

    event Action<UserSession?> AuthStateChanged;
}
