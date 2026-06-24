using System.Diagnostics;
using System.Text.Json;
using RosGrep.Cli.Commands.Results;
using RosGrep.Cli.Definitions;
using RosGrep.Cli.Output.Text;
using RozGrep.Tools.IncomingCalls;
using RozGrep.Tools.Models;

namespace RosGrep.Cli.Commands.IncomingCalls;

internal class IncomingCallsCommand(IncomingCallsTool tool) : ICommand<IncomingCallsCommand.Options>
{
    public async Task<ICommandResult> ExecuteAsync(Options options)
    {
        IncomingCallsToolArgs toolArgs = new()
        {
            WorkspaceName = options.Name,
            TargetName = options.TargetName,
            TargetNamespace = options.TargetNamespace,
            TargetTypeKind = options.TargetTypeKind,
            Depth = options.Depth,
        };

        toolArgs.IncludedMembers.UnionWith(options.IncludedMembers);
        toolArgs.ExcludedMembers.UnionWith(options.ExcludedMembers);
        toolArgs.MemberSymbolKinds.UnionWith(options.MemberSymbolKinds);

        var result = await tool.InvokeAsync(toolArgs);

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

        static string Text(CallableNode c) => c.Signature + (c.Definition is { } d ? $" @ {d}" : "");

        static AsciiTreeNode MapCallerNode(CallerNode c) => new(Text(c), c.Callers.Select(MapCallerNode));
    }

    [Verb("incoming-calls", HelpText = "Find calls recursively to a method")]
    internal sealed class Options
    {
        [Value(0, MetaName = "workspace", Required = true, HelpText = "Path to the solution (.sln[x]) or project (.csproj) to load.")]
        public string Name { get; set; } = "";

        [Value(1, MetaName = "name", Required = true, HelpText = "Name of the type whose members' callers to walk.")]
        public string TargetName { get; set; } = "";

        [Option("kind", HelpText =
            "Kind of the type whose members' callers to walk. " +
            "Use to avoid naming conflicts between different constructs with the same name.")]
        public IncomingCallsToolTargetTypeKind? TargetTypeKind { get; set; }

        [Option("namespace", HelpText =
            "Namespace of the type whose members' callers to walk. " +
            "Use to avoid naming conflicts between different constructs with the same name.")]
        public string? TargetNamespace { get; set; }

        [Option("include-members", HelpText = "Set of included of members to walk.")]
        public IEnumerable<string> IncludedMembers { get; set; } = [];

        [Option("exclude-members", HelpText = "Set of excluded members to walk.")]
        public IEnumerable<string> ExcludedMembers { get; set; } = [];

        [Option("member-kinds", HelpText = "Set of kinds of members to walk.")]
        public IEnumerable<IncomingCallsToolMemberSymbolKind> MemberSymbolKinds { get; set; } = [];

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