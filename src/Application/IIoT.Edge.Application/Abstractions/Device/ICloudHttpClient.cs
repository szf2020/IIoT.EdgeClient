namespace IIoT.Edge.Application.Abstractions.Device;

/// <summary>
/// 云端 HTTP 客户端接口。
///
/// 封装对云端 API 的通用 GET 和 POST 调用。
/// 所有需要访问云端 API 的组件都应依赖该抽象。
/// </summary>
public interface ICloudHttpClient
{
    /// <summary>
    /// 向指定地址提交 JSON 数据。
    /// </summary>
    /// <returns>成功返回 true，失败返回 false，不抛出异常。</returns>
    Task<bool> PostAsync(string url, object payload);

    /// <summary>
    /// 向指定地址提交 JSON 数据，并返回响应内容。
    /// </summary>
    /// <returns>成功时返回响应字符串，失败时返回 null。</returns>
    Task<string?> PostWithResponseAsync(string url, object payload);

    /// <summary>
    /// 发起 GET 请求。
    /// </summary>
    /// <returns>成功时返回响应字符串，失败时返回 null。</returns>
    Task<string?> GetAsync(string url);
}
