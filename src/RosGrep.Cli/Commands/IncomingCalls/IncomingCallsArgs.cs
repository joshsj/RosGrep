using System.ComponentModel;
using RozGrep.Tools.IncomingCalls;
using Spectre.Console.Cli;

namespace RosGrep.Cli.Commands.IncomingCalls;

internal sealed class IncomingCallsArgs : CommandSettings
{
    [CommandArgument(0, "<workspace>")]
    [Description("Path to the solution (.sln, .slnx, .slnx) or project (.csproj) to load.")]
    public string Name { get; set; } = "";

    [CommandArgument(1, "<target>")]
    [Description("Name of the target type whose members' callers to walk.")]
    public string TargetName { get; set; } = "";

    [CommandOption("--target-kind")]
    [Description(
        "Kind of the target type. " +
        "Use to avoid naming conflicts between different constructs with the same name.")]
    public IncomingCallsToolTargetTypeKind? TargetTypeKind { get; set; }

    [CommandOption("--target-namespace")]
    [Description(
        "Namespace of the type whose members' callers to walk. " +
        "Use to avoid naming conflicts between different constructs with the same name.")]
    public string? TargetNamespace { get; set; }

    [CommandOption("--include-member")]
    [Description("Set of included of members to walk.")]
    public string[] IncludedMembers { get; init; } = [];

    [CommandOption("--exclude-member")]
    [Description("Set of excluded members to walk.")]
    public string[] ExcludedMembers { get; init; } = [];

    [CommandOption("--member-kind")]
    [Description("Set of kinds of members to walk.")]
    public IncomingCallsToolMemberSymbolKind[] MemberSymbolKinds { get; init; } = [];

    [CommandOption("-d|--depth")]
    [DefaultValue(15)]
    [Description("Maximum recursion depth.")]
    public int Depth { get; set; }

    [CommandOption("-f|--format")]
    [DefaultValue(default(OutputFormat))]
    [Description($"Output format ({nameof(OutputFormat.Json)}, {nameof(OutputFormat.Tree)})")]
    public OutputFormat Format { get; set; }

    public enum OutputFormat
    {
        Json,
        Tree,
    }
}