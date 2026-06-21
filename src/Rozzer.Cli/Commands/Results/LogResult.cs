namespace Rozzer.Cli.Commands.Results;

internal record LogResult(string Message) : ICommandResult
{
    public static implicit operator LogResult(string message) => new(message);
}