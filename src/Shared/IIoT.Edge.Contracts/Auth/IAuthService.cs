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
        /// 云端设备登录，必须携带已寻址成功的 DeviceId
        /// </summary>
        Task<AuthResult> LoginCloudAsync(
            string employeeNo,
            string password,
            Guid deviceId);

        void Logout();

        event Action<UserSession?> AuthStateChanged;
    }
}
