using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace RosGrep.Tools.MSBuild;

public sealed class MSBuildLocatorInitializer(ILogger<MSBuildLocatorInitializer> logger)
{
    private bool _hasInitialized = false;

    public void EnsureInitialized()
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        if (_hasInitialized)
        {
            throw new InvalidOperationException($"{nameof(MSBuildLocatorInitializer)} has already been initialized");
        }

        // todo make configurable
        var vsInstance = MSBuildLocator.RegisterDefaults();

        logger.LogDebug(
            "Loaded VS instance '{InstanceName}', version={InstanceVersion}, path={InstanceMsBuildPath}",
            vsInstance.Name,
            vsInstance.Version,
            vsInstance.MSBuildPath
        );
        
        _hasInitialized = true;
    }
}