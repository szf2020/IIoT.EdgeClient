// 路径：src/Infrastructure/IIoT.Edge.CloudSync/Auth/AuthService.cs
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace IIoT.Edge.CloudSync.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ICloudApiEndpointProvider _endpointProvider;
        private readonly LocalAdminConfig _localAdminConfig;

        public UserSession? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public event Action<UserSession?>? AuthStateChanged;

        public AuthService(
            HttpClient httpClient,
            ICloudApiEndpointProvider endpointProvider,
            LocalAdminConfig localAdminConfig)
        {
            _httpClient = httpClient;
            _endpointProvider = endpointProvider;
            _localAdminConfig = localAdminConfig;
        }

        public bool HasPermission(string permission)
        {
            if (CurrentUser is null) return false;
            if (CurrentUser.IsLocalAdmin) return true;
            if (CurrentUser.Permissions.Contains("Admin")) return true;
            return CurrentUser.Permissions.Contains(permission);
        }

        public Task<AuthResult> LoginLocalAsync(string password)
        {
            var inputHash = ComputeSha256(password);
            if (inputHash != _localAdminConfig.PasswordHash)
                return Task.FromResult(AuthResult.Fail("密码错误"));

            var session = new UserSession
            {
                DisplayName = "紧急管理员",
                IsLocalAdmin = true,
                Permissions = new HashSet<string>(),
                Token = null
            };

            SetSession(session);
            return Task.FromResult(AuthResult.Ok("本地管理员登录成功"));
        }

        /// <summary>
        /// 云端设备登录：调用 device-login 接口，携带 DeviceId 做设备绑定校验
        /// </summary>
        public async Task<AuthResult> LoginCloudAsync(
            string employeeNo,
            string password,
            Guid deviceId)
        {
            try
            {
                var loginUrl = _endpointProvider.BuildUrl(_endpointProvider.GetIdentityDeviceLoginPath());
                var response = await _httpClient.PostAsJsonAsync(
                    loginUrl,
                    new
                    {
                        employeeNo,
                        password,
                        deviceId
                    });

                if (!response.IsSuccessStatusCode)
                {
                    // 尝试读取云端返回的错误信息
                    var errors = await response.Content
                        .ReadFromJsonAsync<string[]>();

                    var errorMsg = errors?.FirstOrDefault()
                        ?? response.StatusCode switch
                        {
                            System.Net.HttpStatusCode.Unauthorized => "工号或密码错误",
                            System.Net.HttpStatusCode.Forbidden => "您无权操作此设备，请联系管理员绑定设备权限",
                            _ => $"登录失败：{response.StatusCode}"
                        };

                    return AuthResult.Fail(errorMsg);
                }

                var token = await response.Content.ReadAsStringAsync();
                token = token.Trim('"');

                if (string.IsNullOrEmpty(token))
                    return AuthResult.Fail("服务端未返回 Token");

                var session = ParseJwtToken(token);
                if (session is null)
                    return AuthResult.Fail("Token 解析失败");

                SetSession(session);
                return AuthResult.Ok($"欢迎，{session.DisplayName}");
            }
            catch (TaskCanceledException)
            {
                return AuthResult.Fail("连接超时，请检查网络");
            }
            catch (HttpRequestException)
            {
                return AuthResult.Fail("无法连接到服务器，请检查网络");
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"登录异常：{ex.Message}");
            }
        }

        public void Logout() => SetSession(null);

        private void SetSession(UserSession? session)
        {
            CurrentUser = session;
            AuthStateChanged?.Invoke(CurrentUser);
        }

        private static UserSession? ParseJwtToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var displayName = jwtToken.Claims
                    .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)
                    ?.Value ?? "未知用户";

                var permissions = jwtToken.Claims
                    .Where(c => c.Type == "Permission" || c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToHashSet();
                return new UserSession
                {
                    DisplayName = displayName,
                    IsLocalAdmin = false,
                    Permissions = permissions,
                    Token = token
                };
            }
            catch { return null; }
        }

        private static string ComputeSha256(string input)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
