using Microsoft.Extensions.DependencyInjection;
using Rozzer.Tools.IncomingCalls;
using Rozzer.Tools.MSBuild;

namespace Rozzer.Tools;

public static class Configuration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterRozzer()
        {
            services.AddSingleton<MSBuildLocatorInitializer>();

            services.AddTransient<IncomingCallsTool>();

            return services;
        }
    }
}