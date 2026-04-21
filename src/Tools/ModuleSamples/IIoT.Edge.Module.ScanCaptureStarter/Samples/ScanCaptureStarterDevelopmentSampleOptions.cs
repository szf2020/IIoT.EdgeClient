namespace IIoT.Edge.Module.ScanCaptureStarter.Samples;

public sealed class ScanCaptureStarterDevelopmentSampleOptions
{
    public const string SectionName = "DevelopmentSamples";

    public bool Enabled { get; set; }

    public bool SeedScanCaptureStarterModule { get; set; }

    public string StarterDeviceName { get; set; } = "PLC-STARTER-DEV";

    public string StarterIpAddress { get; set; } = "127.0.0.1";

    public int StarterPort { get; set; } = 1103;

    public string StarterPlcModel { get; set; } = "S7";

    public int StarterConnectTimeout { get; set; } = 1000;

    public string SampleBarcode { get; set; } = "STARTER-DEV-0001";

    public int SampleSequence { get; set; } = 1;
}
