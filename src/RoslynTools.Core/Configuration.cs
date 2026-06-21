using Microsoft.Extensions.DependencyInjection;
using RoslynTools.Core.MSBuild;
using RoslynTools.Core.Tools.IncomingCalls;

namespace RoslynTools.Core;

public static class Configuration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterRoslynCore()
        {
            services.AddSingleton<MSBuildLocatorInitializer>();

            services.AddTransient<IncomingCallsTool>();

            return services;
        }
    }
}