using IIoT.Edge.Contracts.Device;
using System.Net.Http.Json;

namespace IIoT.Edge.CloudSync.Http;

/// <summary>
/// 云端 HTTP 客户端实现
/// 
/// 内部使用 IHttpClientFactory 管理连接池
/// 所有异常内部捕获，返回 bool/null，不向上抛
/// </summary>
public class CloudHttpClient : ICloudHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CloudHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> PostAsync(string url, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");
            var response = await client
                .PostAsJsonAsync(url, payload)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> PostWithResponseAsync(string url, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");
            var response = await client
                .PostAsJsonAsync(url, payload)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");
            var response = await client
                .GetAsync(url)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return null;
        }
        catch
        {
            return null;
        }
    }
}