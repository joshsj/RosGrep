namespace RozGrep.Tools.IncomingCalls;

public record IncomingCallsToolOptions
{
    public required string WorkspaceName { get; init; }

    public required string TargetName { get; init; }

    public required string? TargetNamespace { get; init; }

    public required IncomingCallsToolTargetTypeKind? TargetTypeKind { get; init; }

    public HashSet<string> IncludedMembers { get; } = [];

    public HashSet<string> ExcludedMembers { get; } = [];

    public HashSet<IncomingCallsToolMemberSymbolKind> MemberSymbolKinds { get; } = [];

    public required int Depth { get; init; }
}