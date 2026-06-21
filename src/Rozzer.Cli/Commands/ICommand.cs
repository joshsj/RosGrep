namespace Rozzer.Cli.Commands;

internal interface ICommand
{
    // todo is this too restrictive on commands?
    Task<ICommandResult> ExecuteAsync(object options);
}

internal interface ICommand<in T> : ICommand
{
    Task<ICommandResult> ICommand.ExecuteAsync(object options) => ExecuteAsync((T)options);

    Task<ICommandResult> ExecuteAsync(T options);
}
