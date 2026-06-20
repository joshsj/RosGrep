namespace RoslynTools.Cli.Commands.Results;

internal record ErrorResult(string Message) : ICommandResult;