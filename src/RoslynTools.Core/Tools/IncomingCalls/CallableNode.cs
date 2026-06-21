namespace RoslynTools.Core.Tools.IncomingCalls;

// todo better name?
public class CallableNode(string signature) : IComparable<CallableNode>
{
    public string Signature { get; init; } = signature;

    public SymbolLocation? Definition => Definitions.Count == 1 ? Definitions[0] : null;
    
    public List<SymbolLocation> Definitions { get; } = [];
    
    public List<CallerNode> Callers { get; } = [];

    public int CompareTo(CallableNode? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        return string.Compare(Signature, other.Signature, StringComparison.Ordinal);
    }
}