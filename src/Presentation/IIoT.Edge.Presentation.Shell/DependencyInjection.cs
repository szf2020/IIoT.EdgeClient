using IIoT.Edge.Presentation.Shell.Features.Footer;
using IIoT.Edge.Presentation.Shell.Features.Header;
using IIoT.Edge.Presentation.Shell.Features.Login;
using IIoT.Edge.Presentation.Shell.Features.SysMenu;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Presentation.Shell;

public static class DependencyInjection
{
    public static IServiceCollection AddShellPresentation(this IServiceCollection services)
    {
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<SysMenuViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<FooterViewModel>();
        return services;
    }
}
