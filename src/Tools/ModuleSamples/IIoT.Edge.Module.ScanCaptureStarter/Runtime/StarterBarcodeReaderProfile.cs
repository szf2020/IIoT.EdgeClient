using IIoT.Edge.Application.Abstractions.Device;

namespace IIoT.Edge.Module.ScanCaptureStarter.Runtime;

public static class StarterBarcodeReaderProfile
{
    public static PlcBarcodeReaderOptions DefaultOptions { get; } = new()
    {
        StartAddress = "DB1.DBW100",
        CodeCount = 1,
        WordsPerCode = 12,
        EncodingName = "ASCII"
    };
}
