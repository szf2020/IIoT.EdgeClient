using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.UI.Shared.Modularity
{
    public interface IEdgeModule
    {
        string ModuleName { get; }

        void ConfigureServices(IServiceCollection services);

        void ConfigureViews(IViewRegistry registry);
    }
}