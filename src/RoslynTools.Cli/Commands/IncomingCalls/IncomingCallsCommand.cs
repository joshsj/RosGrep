using System.Diagnostics;
using System.Text.Json;
using RoslynTools.Cli.Commands.Results;
using RoslynTools.Cli.Definitions;
using RoslynTools.Cli.Output.Text;
using RoslynTools.Core.Tools.IncomingCalls;

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
                    Options.OutputFormat.Tree => ToAsciiTree(result.Report),
                    Options.OutputFormat.Json => JsonSerializer.Serialize(result.Report,
                        Constants.Formatting.PrettyJsonOptions),
                    _ => throw new UnreachableException()
                }
            ),

            { IsSuccess: false } => new ErrorResult(result.Message),
        };
    }

    private static string ToAsciiTree(IncomingCallsReport report)
    {
        var roots = report.Members.Select(m => new AsciiTreeNode(Text(m), m.Callers.Select(MapCallerNode)));

        return AsciiTreeFormatter.Format(roots);

        static string Text(CallableNode c)
        {
            var def = c.Definition is { } d
                ? $" @ {d.File}"
                : "";

            return c.Signature + def;
        }

        static AsciiTreeNode MapCallerNode(CallerNode c) => new(Text(c), c.Callers.Select(MapCallerNode));
    }

    [Verb("incoming-calls", HelpText = "Find calls recursively to a method")]
    internal sealed class Options
    {
        [Value(0, MetaName = "target", Required = true,
            HelpText = "Path to the solution (.sln[x]) or project (.csproj) to load.")]
        public string Target { get; set; } = "";

        [Value(1, MetaName = "type-name", Required = true,
            HelpText = "Name of the interface whose members' callers to walk.")]
        public string TypeName { get; set; } = "";

        [Option("depth", Default = 15, HelpText = "Maximum recursion depth.")]
        public int Depth { get; set; }

        [Option("format", Default = default(OutputFormat),
            HelpText = $"Output format, {nameof(OutputFormat.Json)} or {nameof(OutputFormat.Tree)}")]
        public OutputFormat Format { get; set; }

        public enum OutputFormat
        {
            Json,
            Tree,
        }
    }
}