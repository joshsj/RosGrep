namespace RosGrep.Cli.Commands;

// todo better name but can't be ICommand cos Spectre has claimed that
internal interface IRosGrepCommand<in TArgs>
{
    // todo is this too restrictive on commands?
    Task<ICommandResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken);
}
