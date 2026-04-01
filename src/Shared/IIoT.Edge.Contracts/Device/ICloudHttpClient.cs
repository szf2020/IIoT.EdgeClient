namespace IIoT.Edge.Contracts.Device;

/// <summary>
/// 云端 HTTP 客户端接口
/// 
/// 封装对云端 API 的通用 POST/GET 操作
/// 接口在 Contracts 层，实现在 CloudSync 层
/// 
/// 所有需要调云端 API 的地方统一注入这个接口：
///   CloudConsumer、CapacitySyncTask、DeviceLogSyncTask、RetryTask
///   后续 MES 可加 IMesHttpClient 同理
/// </summary>
public interface ICloudHttpClient
{
    /// <summary>
    /// POST JSON 到云端 API
    /// </summary>
    /// <returns>成功返回 true，失败返回 false（不抛异常）</returns>
    Task<bool> PostAsync(string url, object payload);

    /// <summary>
    /// POST JSON 并返回响应内容
    /// </summary>
    /// <returns>成功返回响应字符串，失败返回 null</returns>
    Task<string?> PostWithResponseAsync(string url, object payload);

    /// <summary>
    /// GET 请求
    /// </summary>
    /// <returns>成功返回响应字符串，失败返回 null</returns>
    Task<string?> GetAsync(string url);
}