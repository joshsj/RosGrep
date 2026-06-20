namespace RoslynTools.Tools.IncomingCalls;

public record CallerNode
{
    public required string Signature { get; init; }

    public required bool FoundAtMaxDepth { get; init; }
    
    public HashSet<CallSite> CallSites { get; } = [];

    public List<CallerNode> Callers { get; } = [];
}