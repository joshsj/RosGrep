namespace RoslynTools.Core.Tools.IncomingCalls;

public class CallerNode
{
    public required string Signature { get; init; }

    public List<CallSite> CallSites { get; } = [];

    public List<CallerNode> Callers { get; } = [];
}