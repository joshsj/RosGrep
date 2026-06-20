using System.Diagnostics;
using System.Text.Json;
using RoslynTools.Cli.Commands.Results;
using RoslynTools.Cli.Definitions;
using RoslynTools.Tools.IncomingCalls;

namespace RoslynTools.Cli.Commands.IncomingCalls;

internal class IncomingCallsCommand(IncomingCallsTool tool) : ICommand<IncomingCallsCommand.Options>
{
    public async Task<ICommandResult> ExecuteAsync(Options options)
    {
        IncomingCallsToolOptions incomingCallsToolOptions = new()
        {
            SolutionName = options.Target,
            TypeName = options.TypeName,
            Depth = options.Depth,
        };

        var result = await tool.InvokeAsync(incomingCallsToolOptions);

        return result switch
        {
            { IsSuccess: true } => new LogResult(
                options.Format switch
                {
                    Options.OutputFormat.Tree => "to do",
                    Options.OutputFormat.Json => JsonSerializer.Serialize(result.Report, Constants.Formatting.PrettyJsonOptions),
                    _ => throw new UnreachableException()
                }
            ),
                    
            { IsSuccess: false } => new ErrorResult(result.Message),
        };
    }

    [Verb("incoming-calls", HelpText = "Find calls recursively to a method")]
    internal sealed class Options
    {
        [Value(0, MetaName = "target", Required = true,
            HelpText = "Path to the solution (.sln[x]) or project (.csproj) to load. Prefer the solution.")]
        public string Target { get; set; } = "";

        [Value(1, MetaName = "type-name", Required = true,
            HelpText = "Name of the interface whose members' callers to walk (e.g. IAccountStore).")]
        public string TypeName { get; set; } = "";

        [Option("depth", Default = 15, HelpText = "Maximum recursion depth.")]
        public int Depth { get; set; }

        [Option("include-tests", Default = false,
            HelpText = "Include callers in *Tests types/assemblies (excluded by default).")]
        public bool IncludeTests { get; set; }

        [Option("format", Default = OutputFormat.Tree, HelpText = "Output format: text or json.")]
        public OutputFormat Format { get; set; }

        [Option("output", HelpText = "Write the report to this file instead of the console.")]
        public string? Output { get; set; }

        [Option("members",
            HelpText = "Comma-separated member names to restrict the walk to (e.g. Save). Default: all members.")]
        public string? Members { get; set; }

        public enum OutputFormat
        {
            Tree,
            Json,
        }
    }
}