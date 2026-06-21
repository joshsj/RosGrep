using Microsoft.Extensions.DependencyInjection;
using RozGrep.Tools.IncomingCalls;
using RozGrep.Tools.MSBuild;

namespace RozGrep.Tools;

public static class Configuration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterRosGrep()
        {
            services.AddSingleton<MSBuildLocatorInitializer>();

            services.AddTransient<IncomingCallsTool>();

            return services;
        }
    }
}