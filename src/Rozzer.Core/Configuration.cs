using Microsoft.Extensions.DependencyInjection;
using Rozzer.Core.MSBuild;
using Rozzer.Core.Tools.IncomingCalls;

namespace Rozzer.Core;

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