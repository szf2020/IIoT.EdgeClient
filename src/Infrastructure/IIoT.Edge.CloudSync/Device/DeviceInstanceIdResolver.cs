using IIoT.Edge.CloudSync.Config;
using IIoT.Edge.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IIoT.Edge.CloudSync.Device;

public class DeviceInstanceIdResolver : IDeviceInstanceIdResolver
{
    private static readonly string[] InvalidTokens =
    [
        "TOBEFILLEDBYOEM",
        "DEFAULTSTRING",
        "SYSTEMSERIALNUMBER",
        "UNKNOWN",
        "NONE",
        "N/A",
        "NA",
        "NULL"
    ];

    private static readonly string[] VirtualAdapterHints =
    [
        "VIRTUAL",
        "VMWARE",
        "VBOX",
        "HYPER-V",
        "HYPERV",
        "DOCKER",
        "TAP",
        "TUN",
        "WINTUN",
        "BLUETOOTH"
    ];

    private readonly IOptionsMonitor<DeviceIdentityConfig> _identityOptions;
    private readonly ILogService _logger;

    public DeviceInstanceIdResolver(
        IOptionsMonitor<DeviceIdentityConfig> identityOptions,
        ILogService logger)
    {
        _identityOptions = identityOptions;
        _logger = logger;
    }

    public string ResolveInstanceId()
    {
        var configured = NormalizeToken(_identityOptions.CurrentValue.InstanceIdOverride);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _logger.Info("[DeviceIdentity] 使用配置固定实例标识");
            return configured;
        }

        if (_identityOptions.CurrentValue.PreferHardwareFingerprint)
        {
            var fingerprint = BuildHardwareFingerprint();
            if (!string.IsNullOrWhiteSpace(fingerprint))
            {
                _logger.Info("[DeviceIdentity] 使用硬件指纹实例标识");
                return fingerprint;
            }
        }

        var mac = GetStableMacAddress();
        _logger.Warn("[DeviceIdentity] 回退到稳定 MAC 实例标识");
        return mac;
    }

    private static string? BuildHardwareFingerprint()
    {
        var parts = new List<string>
        {
            NormalizeToken(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemUUID")),
            NormalizeToken(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardSerialNumber")),
            NormalizeToken(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemSerialNumber")),
            NormalizeToken(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid")),
            NormalizeToken(ReadRegistryValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "Identifier"))
        };

        var valid = parts
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (valid.Count == 0)
            return null;

        var material = string.Join("|", valid);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        var hex = Convert.ToHexString(hash);
        return "HW" + hex[..30];
    }

    private static string GetStableMacAddress()
    {
        var adapters = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                          && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => new
            {
                Nic = nic,
                Mac = NormalizeToken(nic.GetPhysicalAddress().ToString())
            })
            .Where(x => IsValidMac(x.Mac))
            .OrderByDescending(x => ScoreNic(x.Nic))
            .ThenBy(x => x.Mac, StringComparer.Ordinal)
            .ToList();

        return adapters.FirstOrDefault()?.Mac ?? "000000000000";
    }

    private static int ScoreNic(NetworkInterface nic)
    {
        var score = nic.NetworkInterfaceType switch
        {
            NetworkInterfaceType.Ethernet => 300,
            NetworkInterfaceType.GigabitEthernet => 300,
            NetworkInterfaceType.Wireless80211 => 200,
            _ => 100
        };

        var name = $"{nic.Name} {nic.Description}".ToUpperInvariant();
        if (VirtualAdapterHints.Any(h => name.Contains(h, StringComparison.Ordinal)))
            score -= 150;

        return score;
    }

    private static bool IsValidMac(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Regex.IsMatch(value, "^[0-9A-F]{12}$", RegexOptions.CultureInvariant))
            return false;

        if (value == "000000000000" || value == "FFFFFFFFFFFF")
            return false;

        return true;
    }

    private static string? ReadRegistryValue(string key, string name)
    {
        try
        {
            return Registry.GetValue(key, name, null)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var normalized = raw.Trim().ToUpperInvariant();
        normalized = normalized.Replace("-", string.Empty, StringComparison.Ordinal)
                               .Replace(":", string.Empty, StringComparison.Ordinal)
                               .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (normalized.Length < 6)
            return string.Empty;

        if (normalized.All(c => c == '0') || normalized.All(c => c == 'F'))
            return string.Empty;

        if (InvalidTokens.Contains(normalized, StringComparer.Ordinal))
            return string.Empty;

        return normalized;
    }
}

