using Microsoft.Extensions.DependencyInjection;
using RosGrep.Tools.IncomingCalls;
using RosGrep.Tools.MSBuild;

namespace RosGrep.Tools;

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