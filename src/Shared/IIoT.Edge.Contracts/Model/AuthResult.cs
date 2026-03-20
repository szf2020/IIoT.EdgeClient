// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/AuthResult.cs
namespace IIoT.Edge.Contracts.Model
{
    /// <summary>登录操作结果</summary>
    public class AuthResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;

        public static AuthResult Ok(string message = "登录成功")
            => new() { Success = true, Message = message };

        public static AuthResult Fail(string message)
            => new() { Success = false, Message = message };
    }
}