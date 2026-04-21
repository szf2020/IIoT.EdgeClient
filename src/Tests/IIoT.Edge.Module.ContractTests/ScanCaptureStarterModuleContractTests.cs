using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.Module.ScanCaptureStarter;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.ContractTests;

public sealed class ScanCaptureStarterModuleContractTests : ModuleContractTestBase<ScanCaptureStarterModule>
{
    protected override bool RequiresHardwareProfile => true;
    protected override int ExpectedRuntimeTaskCount => 2;
    protected override int MinimumRouteCount => 3;

    protected override void ConfigureRuntimeServices(IServiceCollection services)
    {
        AddDefaultRuntimeServices(services);
        services.AddSingleton<IBarcodeReaderFactory, StubBarcodeReaderFactory>();
    }

    private sealed class StubBarcodeReaderFactory : IBarcodeReaderFactory
    {
        public IBarcodeReader Create(int networkDeviceId, PlcBarcodeReaderOptions options)
            => new StubBarcodeReader();
    }

    private sealed class StubBarcodeReader : IBarcodeReader
    {
        public Task<string[]> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<string>());
    }
}
