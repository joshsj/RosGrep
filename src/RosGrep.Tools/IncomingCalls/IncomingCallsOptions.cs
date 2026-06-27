namespace RosGrep.Tools.IncomingCalls;

public record IncomingCallsToolArgs
{
    public required string WorkspaceName { get; init; }

    public required int Depth { get; init; }

    public required string TargetName { get; init; }

    public string? TargetNamespace { get; init; }

    public IncomingCallsToolTargetTypeKind? TargetTypeKind { get; init; }

    // todo not sure about mutable collections on a record
    // but I also don't like the perf implications of re-assigning the whole list just to update an instance
    public HashSet<string> IncludedMembers { get; } = [];

    public HashSet<string> ExcludedMembers { get; } = [];

    public HashSet<IncomingCallsToolMemberSymbolKind> MemberSymbolKinds { get; } = [];
}