// 路径：src/Infrastructure/IIoT.Edge.CloudSync/Auth/AuthService.cs
using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace IIoT.Edge.CloudSync.Auth
{
    /// <summary>
    /// IAuthService 实现。
    /// 双轨登录：本地紧急管理员（断网保底）+ 云端 JWT 登录。
    /// 完全依赖 IIoT.Edge.Contracts 抽象，不依赖任何 UI 层。
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalAdminConfig _localAdminConfig;

        public UserSession? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public event Action<UserSession?>? AuthStateChanged;

        public AuthService(HttpClient httpClient, LocalAdminConfig localAdminConfig)
        {
            _httpClient = httpClient;
            _localAdminConfig = localAdminConfig;
        }

        // ── 权限判断 ─────────────────────────────────────────────────
        public bool HasPermission(string permission)
        {
            if (CurrentUser is null) return false;
            if (CurrentUser.IsLocalAdmin) return true;
            // 云端 Admin 角色全放行
            if (CurrentUser.Permissions.Contains("Admin")) return true;
            return CurrentUser.Permissions.Contains(permission);
        }

        // ── 本地紧急管理员登录 ────────────────────────────────────────
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

        // ── 云端账号登录 ──────────────────────────────────────────────
        public async Task<AuthResult> LoginCloudAsync(string employeeNo, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/v1/Identity/login",
                    new { employeeNo, password });

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "工号或密码错误"
                        : $"登录失败：{response.StatusCode}";
                    return AuthResult.Fail(errorMsg);
                }

                // 云端直接返回 JWT 字符串
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
            catch (HttpRequestException)
            {
                return AuthResult.Fail("无法连接到服务器，请检查网络");
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"登录异常：{ex.Message}");
            }
        }

        // ── 注销 ──────────────────────────────────────────────────────
        public void Logout()
        {
            SetSession(null);
        }

        // ── 私有方法 ───────────────────────────────────────────────────
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

                // 同时读 Permission 和 Role Claims
                var permissions = jwtToken.Claims
                    .Where(c => c.Type == "Permission" ||
                                c.Type == ClaimTypes.Role)
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
            catch
            {
                return null;
            }
        }

        private static string ComputeSha256(string input)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}