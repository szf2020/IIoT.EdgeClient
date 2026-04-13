namespace IIoT.Edge.CloudSync.Config;

public class CloudApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSecs { get; set; } = 10;
    public string ClientCode { get; set; } = string.Empty;
    public CloudApiPaths Paths { get; set; } = new();
}

public class CloudApiPaths
{
    public string DeviceInstance { get; set; } = "/api/v1/Device/instance";
    public string IdentityDeviceLogin { get; set; } = "/api/v1/Identity/device-login";
    public string PassStationInjectionBatch { get; set; } = "/api/v1/PassStation/injection/batch";
    public string DeviceLog { get; set; } = "/api/v1/DeviceLog";
    public string CapacityHourly { get; set; } = "/api/v1/Capacity/hourly";
    public string CapacitySummary { get; set; } = "/api/v1/Capacity/summary";
    public string CapacitySummaryRange { get; set; } = "/api/v1/Capacity/summary/range";
    public string RecipeByDeviceTemplate { get; set; } = "/api/v1/Recipe/device/{deviceId}";
}
