using System.Diagnostics;
using System.Text.Json;
using RosGrep.Cli.Commands.Results;
using RosGrep.Cli.Definitions;
using RosGrep.Cli.Output.Text;
using RosGrep.Tools.IncomingCalls;
using RosGrep.Tools.Models;

namespace RosGrep.Cli.Commands.IncomingCalls;

internal class IncomingCallsCommand(IncomingCallsTool tool) : ICommand<IncomingCallsArgs>
{
    public async Task<ICommandResult> ExecuteAsync(IncomingCallsArgs args, CancellationToken cancellationToken)
    {
        IncomingCallsToolArgs toolArgs = new()
        {
            WorkspaceName = args.Name,
            TargetName = args.TargetName,
            TargetNamespace = args.TargetNamespace,
            TargetTypeKind = args.TargetTypeKind,
            Depth = args.Depth,
        };

        toolArgs.IncludedMembers.UnionWith(args.IncludedMembers);
        toolArgs.ExcludedMembers.UnionWith(args.ExcludedMembers);
        toolArgs.MemberSymbolKinds.UnionWith(args.MemberSymbolKinds);

        var result = await tool.InvokeAsync(toolArgs, cancellationToken);

        return result switch
        {
            { IsSuccess: true } => new LogResult(
                args.Format switch
                {
                    // todo slap dash, do we want to make this re-usable as well
                    IncomingCallsArgs.OutputFormat.Tree => ToAsciiTree(result.Report),
                    IncomingCallsArgs.OutputFormat.Json => JsonSerializer.Serialize(result.Report, Constants.Formatting.PrettyJsonOptions),
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
}
