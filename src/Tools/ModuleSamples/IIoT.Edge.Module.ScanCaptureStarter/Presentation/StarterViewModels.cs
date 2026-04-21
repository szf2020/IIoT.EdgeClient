using IIoT.Edge.Application.Abstractions.Auth;
using IIoT.Edge.Application.Features.Config.ParamView;
using IIoT.Edge.Application.Features.Hardware.HardwareConfigView;
using IIoT.Edge.Presentation.Navigation.Features.Config.ParamView;
using IIoT.Edge.Presentation.Navigation.Features.Hardware.HardwareConfigView;

namespace IIoT.Edge.Module.ScanCaptureStarter.Presentation;

public sealed class StarterParamViewModel : ParamViewModel
{
    public StarterParamViewModel(
        IParamViewCrudService crudService,
        IClientPermissionService permissionService)
        : base(crudService, permissionService, StarterViewIds.ParamView, "Starter Param Config")
    {
    }
}

public sealed class StarterHardwareConfigViewModel : HardwareConfigViewModel
{
    public StarterHardwareConfigViewModel(
        IHardwareConfigCrudService crudService,
        IClientPermissionService permissionService)
        : base(crudService, permissionService, StarterViewIds.HardwareConfigView, "Starter Hardware Config")
    {
    }
}
