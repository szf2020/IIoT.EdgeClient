using IIoT.Edge.Infrastructure.Integration.Config;
using IIoT.Edge.Application.Abstractions.Device;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IIoT.Edge.Infrastructure.Integration.Http;

/// <summary>
/// Cloud HTTP client.
/// Catches exceptions and returns bool/null.
/// </summary>
public class CloudHttpClient : ICloudHttpClient
{
    private static readonly HashSet<string> BlockedIdentityKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "macAddress",
        "mac_address",
        "clientCode",
        "client_code"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICloudApiEndpointProvider _endpointProvider;

    public CloudHttpClient(
        IHttpClientFactory httpClientFactory,
        ICloudApiEndpointProvider endpointProvider)
    {
        _httpClientFactory = httpClientFactory;
        _endpointProvider = endpointProvider;
    }

    public async Task<bool> PostAsync(string url, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CloudApi");
            var requestUrl = _endpointProvider.BuildUrl(url);
            var sanitizedPayload = SanitizePayload(payload);

            var response = await client
                .PostAsJsonAsync(requestUrl, sanitizedPayload)
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
            var requestUrl = _endpointProvider.BuildUrl(url);
            var sanitizedPayload = SanitizePayload(payload);

            var response = await client
                .PostAsJsonAsync(requestUrl, sanitizedPayload)
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
            var requestUrl = _endpointProvider.BuildUrl(url);
            var response = await client
                .GetAsync(requestUrl)
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

    private static object SanitizePayload(object payload)
    {
        JsonNode? node;

        try
        {
            node = JsonSerializer.SerializeToNode(payload);
        }
        catch
        {
            return payload;
        }

        if (node is null)
            return payload;

        RemoveBlockedKeysRecursively(node);
        return node;
    }

    private static void RemoveBlockedKeysRecursively(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keysToRemove = obj
                .Select(kv => kv.Key)
                .Where(k => BlockedIdentityKeys.Contains(k))
                .ToList();

            foreach (var key in keysToRemove)
                obj.Remove(key);

            foreach (var kv in obj.ToList())
            {
                if (kv.Value is not null)
                    RemoveBlockedKeysRecursively(kv.Value);
            }

            return;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null)
                    RemoveBlockedKeysRecursively(item);
            }
        }
    }
}
