using Microsoft.Extensions.DependencyInjection;
using RoslynTools.MSBuild;
using RoslynTools.Tools.IncomingCalls;

namespace RoslynTools;

public static class Configuration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterRoslynTools()
        {
            services.AddSingleton<MSBuildLocatorInitializer>();

            services.AddTransient<IncomingCallsTool>();

            return services;
        }
    }
}