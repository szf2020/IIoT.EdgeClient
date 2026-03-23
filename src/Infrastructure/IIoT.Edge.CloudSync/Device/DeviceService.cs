// 路径：src/Infrastructure/IIoT.Edge.CloudSync/Device/DeviceService.cs
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using IIoT.Edge.Contracts.Device;

namespace IIoT.Edge.CloudSync.Device
{
    public class DeviceService : IDeviceService
    {
        private readonly HttpClient _httpClient;

        public DeviceSession? CurrentDevice { get; private set; }
        public bool IsIdentified => CurrentDevice is not null;

        public event Action<DeviceSession?>? DeviceIdentified;

        public DeviceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IdentifyAsync()
        {
            var mac = GetMacAddress();
            System.Diagnostics.Debug.WriteLine($"[DeviceService] 本机MAC: {mac}");

            try
            {
                var response = await _httpClient
                    .GetAsync($"/api/v1/Device/mac/{mac}")
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DeviceService] 云端寻址失败: {response.StatusCode}");
                    return TryLoadFromCache(mac);
                }

                var dto = await response.Content
                    .ReadFromJsonAsync<DeviceResponseDto>()
                    .ConfigureAwait(false);

                if (dto is null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[DeviceService] 云端返回数据为空");
                    return TryLoadFromCache(mac);
                }

                var session = new DeviceSession
                {
                    DeviceId = dto.Id,
                    DeviceCode = dto.DeviceCode,
                    DeviceName = dto.DeviceName,
                    MacAddress = mac,
                    ProcessId = dto.ProcessId
                };

                CurrentDevice = session;
                DeviceIdentified?.Invoke(CurrentDevice);

                // 写入本地缓存
                SaveToCache(session);

                System.Diagnostics.Debug.WriteLine(
                    $"[DeviceService] 寻址成功: {session.DeviceCode}");
                return true;
            }
            catch (TaskCanceledException)
            {
                // 3秒超时
                System.Diagnostics.Debug.WriteLine(
                    "[DeviceService] 云端寻址超时（3秒），尝试读取本地缓存");
                return TryLoadFromCache(mac);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DeviceService] 网络异常: {ex.Message}，尝试读取本地缓存");
                return TryLoadFromCache(mac);
            }
        }

        // ── 读取本机 MAC 地址 ─────────────────────────────────────────
        private static string GetMacAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                              && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault() ?? "000000000000";
        }

        // ── 本地缓存（简单文件缓存，后期可换 SQLite） ─────────────────
        private static readonly string CacheFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "device_cache.json");

        private void SaveToCache(DeviceSession session)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(session);
                File.WriteAllText(CacheFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] 缓存写入失败: {ex.Message}");
            }
        }

        private bool TryLoadFromCache(string mac)
        {
            try
            {
                if (!File.Exists(CacheFile)) return false;

                var json = File.ReadAllText(CacheFile);
                var session = System.Text.Json.JsonSerializer.Deserialize<DeviceSession>(json);

                if (session is null) return false;

                // 用缓存数据，标记为离线模式
                CurrentDevice = session with { MacAddress = mac };
                DeviceIdentified?.Invoke(CurrentDevice);

                System.Diagnostics.Debug.WriteLine(
                    $"[DeviceService] 使用本地缓存: {session.DeviceCode}（离线模式）");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] 缓存读取失败: {ex.Message}");
                return false;
            }
        }

        // ── 云端返回的 DTO ────────────────────────────────────────────
        private sealed class DeviceResponseDto
        {
            public Guid Id { get; set; }
            public string DeviceCode { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public Guid ProcessId { get; set; }
        }
    }
}