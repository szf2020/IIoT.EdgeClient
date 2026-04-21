using IIoT.Edge.Application.Abstractions.Auth;
using IIoT.Edge.Presentation.Navigation.Features.Config.ParamView;
using IIoT.Edge.Presentation.Navigation.Features.Hardware.HardwareConfigView;
using IIoT.Edge.Module.ScanCaptureStarter.Presentation.ViewModels;
using IIoT.Edge.Module.ScanCaptureStarter.Presentation.Views;
using IIoT.Edge.UI.Shared.Modularity;

namespace IIoT.Edge.Module.ScanCaptureStarter.Presentation;

public static class StarterNavigationRegistration
{
    public static IViewRegistry RegisterScanCaptureStarterViews(this IViewRegistry registry)
    {
        registry.RegisterRoute(StarterViewIds.Skeleton, typeof(ScanCaptureStarterSkeletonPage), typeof(StarterSkeletonViewModel), cacheView: true);
        registry.RegisterRoute(StarterViewIds.ParamView, typeof(ParamViewPage), typeof(StarterParamViewModel), cacheView: false);
        registry.RegisterRoute(StarterViewIds.HardwareConfigView, typeof(HardwareConfigPage), typeof(StarterHardwareConfigViewModel), cacheView: false);

        registry.RegisterMenu(new MenuInfo
        {
            Title = "Starter Dashboard",
            ViewId = StarterViewIds.Skeleton,
            Icon = "ViewDashboardOutline",
            Order = 30,
            RequiredPermission = string.Empty
        });
        registry.RegisterMenu(new MenuInfo
        {
            Title = "Starter Params",
            ViewId = StarterViewIds.ParamView,
            Icon = "Cog",
            Order = 31,
            RequiredPermission = Permissions.ParamConfig
        });
        registry.RegisterMenu(new MenuInfo
        {
            Title = "Starter Hardware",
            ViewId = StarterViewIds.HardwareConfigView,
            Icon = "ServerNetwork",
            Order = 32,
            RequiredPermission = Permissions.HardwareConfig
        });

        return registry;
    }
}
