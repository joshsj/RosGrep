using Microsoft.Extensions.DependencyInjection;
using RosGrep.Cli.Commands;
using Spectre.Console.Cli;

namespace RosGrep.Cli.Spectre;

// todo is this a bit extra? don't have any scoped deps rn
internal sealed class SpectreCommandAdapter
{
    private readonly IServiceProvider _serviceProvider;

    private SpectreCommandAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static Func<CommandContext, TArgs, CancellationToken, Task<int>> CreateDelegate<TCommand, TArgs>(
        IServiceProvider serviceProvider
    )
        where TCommand : IRosGrepCommand<TArgs>
        => new SpectreCommandAdapter(serviceProvider).HandleAsync<TCommand, TArgs>;

    private async Task<int> HandleAsync<TCommand, TArgs>(
        CommandContext context,
        TArgs args,
        CancellationToken cancellationToken
    )
        where TCommand : IRosGrepCommand<TArgs>
    {
        // Use service provider cos Spectre has some weird DI stuff that doesn't support scoped dependencies
        await using var serviceScope = _serviceProvider.CreateAsyncScope();
        var serviceProvider = serviceScope.ServiceProvider;

        var commandHandler = serviceProvider.GetRequiredService<CommandHandler<TCommand, TArgs>>();

        return await commandHandler.HandleAsync(args, cancellationToken);
    }
}