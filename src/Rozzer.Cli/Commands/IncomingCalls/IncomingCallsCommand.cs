using System.Diagnostics;
using System.Text.Json;
using Rozzer.Cli.Commands.Results;
using Rozzer.Cli.Definitions;
using Rozzer.Cli.Output.Text;
using Rozzer.Tools.IncomingCalls;
using Rozzer.Tools.Models;

namespace Rozzer.Cli.Commands.IncomingCalls;

internal class IncomingCallsCommand(IncomingCallsTool tool) : ICommand<IncomingCallsCommand.Options>
{
    public async Task<ICommandResult> ExecuteAsync(Options options)
    {
        IncomingCallsToolOptions incomingCallsToolOptions = new()
        {
            WorkspaceName = options.Name,
            SymbolName = options.SymbolName,
            SymbolType = options.SymbolType,
            SymbolNamespace = options.SymbolNamespace,
            Depth = options.Depth,
        };

        var result = await tool.InvokeAsync(incomingCallsToolOptions);

        return result switch
        {
            { IsSuccess: true } => new LogResult(
                options.Format switch
                {
                    // todo slap dash, do we want to make this re-usable as well
                    Options.OutputFormat.Tree => ToAsciiTree(result.Report),
                    Options.OutputFormat.Json => JsonSerializer.Serialize(result.Report, Constants.Formatting.PrettyJsonOptions),
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
                ? $" ({d.File}:{d.Line})"
                : "";

            return c.Signature + def;
        }

        static AsciiTreeNode MapCallerNode(CallerNode c) => new(Text(c), c.Callers.Select(MapCallerNode));
    }

    [Verb("incoming-calls", HelpText = "Find calls recursively to a method")]
    internal sealed class Options
    {
        [Value(0, MetaName = "workspace", Required = true, HelpText = "Path to the solution (.sln[x]) or project (.csproj) to load.")]
        public string Name { get; set; } = "";

        [Value(1, MetaName = "symbol-name", Required = true, HelpText = "Name of the symbol whose members' callers to walk.")]
        public string SymbolName { get; set; } = "";

        [Option("symbol-type",
            HelpText = "Type of the symbol whose members' callers to walk. " + 
                       "Use to avoid naming conflicts between different constructs with the same name.")]
        public IncomingCallsToolSymbolType SymbolType { get; set; }
        
        [Option("symbol-namespace",
            HelpText = "Namespace of the symbol whose members' callers to walk. " + 
                       "Use to avoid naming conflicts between different constructs with the same name.")]
        public string? SymbolNamespace { get; set; }

        [Option("depth", Default = 15, HelpText = "Maximum recursion depth.")]
        public int Depth { get; set; }

        [Option("format", HelpText = $"Output format, {nameof(OutputFormat.Json)} or {nameof(OutputFormat.Tree)}")]
        public OutputFormat Format { get; set; }

        public enum OutputFormat
        {
            Json,
            Tree,
        }
    }
}