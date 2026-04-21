using IIoT.Edge.Application.Abstractions.Modules;
using IIoT.Edge.Module.Abstractions;
using IIoT.Edge.Module.ScanCaptureStarter.Constants;
using IIoT.Edge.Module.ScanCaptureStarter.Integration;
using IIoT.Edge.Module.ScanCaptureStarter.Payload;
using IIoT.Edge.Module.ScanCaptureStarter.Presentation;
using IIoT.Edge.Module.ScanCaptureStarter.Runtime;
using IIoT.Edge.Module.ScanCaptureStarter.Samples;
using IIoT.Edge.UI.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Module.ScanCaptureStarter;

public sealed class ScanCaptureStarterModule : IEdgeStationModule
{
    public string ModuleId => StarterModuleConstants.ModuleId;

    public string ProcessType => StarterModuleConstants.ProcessType;

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IProcessCloudUploader, StarterCloudUploader>();
        services.AddSingleton<IModuleHardwareProfileProvider, StarterHardwareProfileProvider>();
        services.AddSingleton<IDevelopmentSampleContributor, ScanCaptureStarterDevelopmentSampleContributor>();
        services.AddSingleton<Presentation.ViewModels.StarterSkeletonViewModel>();
        services.AddSingleton<Presentation.StarterParamViewModel>();
        services.AddSingleton<Presentation.StarterHardwareConfigViewModel>();
    }

    public void RegisterViews(IViewRegistry viewRegistry)
    {
        viewRegistry.RegisterScanCaptureStarterViews();
    }

    public void RegisterCellData(ICellDataRegistry registry)
    {
        registry.Register<StarterCellData>(ProcessType);
    }

    public void RegisterRuntime(IStationRuntimeRegistry registry)
    {
        registry.Register(new StarterStationRuntimeFactory());
    }

    public void RegisterIntegrations(IProcessIntegrationRegistry registry)
    {
        registry.RegisterCloudUploader(ProcessType, ProcessUploadMode.Single);
    }
}
