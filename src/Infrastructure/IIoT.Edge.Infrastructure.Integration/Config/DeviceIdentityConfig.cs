namespace IIoT.Edge.Infrastructure.Integration.Config;

public class DeviceIdentityConfig
{
    /// <summary>
    /// Optional fixed instance id override. When configured, this value is used directly.
    /// </summary>
    public string InstanceIdOverride { get; set; } = string.Empty;

    /// <summary>
    /// Prefer hardware fingerprint as instance id. If false, fallback to stable MAC strategy.
    /// </summary>
    public bool PreferHardwareFingerprint { get; set; } = true;
}
