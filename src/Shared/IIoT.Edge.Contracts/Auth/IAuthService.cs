// 路径：src/Shared/IIoT.Edge.Contracts/Auth/IAuthService.cs
using IIoT.Edge.Contracts.Model;

namespace IIoT.Edge.Contracts.Auth
{
    public interface IAuthService
    {
        UserSession? CurrentUser { get; }
        bool IsAuthenticated { get; }

        bool HasPermission(string permission);

        Task<AuthResult> LoginLocalAsync(string password);

        /// <summary>
        /// 云端设备登录，deviceId 为 null 时云端会拒绝非Admin登录
        /// </summary>
        Task<AuthResult> LoginCloudAsync(
            string employeeNo,
            string password,
            Guid? deviceId = null);

        void Logout();

        event Action<UserSession?> AuthStateChanged;
    }
}