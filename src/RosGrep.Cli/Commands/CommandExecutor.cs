using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RosGrep.Cli.Commands.Results;

namespace RosGrep.Cli.Commands;

internal sealed class CommandExecutor(IServiceProvider serviceProvider, ILogger< CommandExecutor> logger)
{
    public async Task<int> ExecuteAsync(Type commandType, object options)
    {
        await using var serviceScope = serviceProvider.CreateAsyncScope();

        var command = (ICommand)serviceScope.ServiceProvider.GetRequiredService(commandType);

        var result = await command.ExecuteAsync(options);
        
        switch (result) {
            case LogResult logResult:
            {
                // todo or use logger?
                Console.WriteLine(logResult.Message);
                return 0;
            }

            case ErrorResult errorResult:
            {
                logger.LogError("{Message}", errorResult.Message);
                return 1;
            }

            default: throw new UnreachableException();
        }
    }
}