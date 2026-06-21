using System.Diagnostics.CodeAnalysis;

namespace RoslynTools.Tools.IncomingCalls;

public class MemberNode
{
    public required string Signature { get; init; }

    public List<CallerNode> Callers { get; } = [];

    [SetsRequiredMembers]
    public MemberNode(string signature, IEnumerable<CallerNode> callers)
    {
        Signature = signature;
        Callers.AddRange(callers);
    }
}