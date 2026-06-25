namespace RosGrep.Cli.Commands;

internal interface ICommand<in TArgs>
{
    // todo is this too restrictive on commands?
    Task<ICommandResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken);
}
