using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RosGrep.Cli.Commands.Results;

namespace RosGrep.Cli.Commands;

internal sealed class CommandHandler<TCommand, TArgs>(TCommand command, ILogger<CommandHandler<TCommand, TArgs>> logger)
    where TCommand : IRosGrepCommand<TArgs>
{
    public async Task<int> HandleAsync(TArgs args, CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(args, cancellationToken);
        
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