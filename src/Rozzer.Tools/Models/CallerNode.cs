namespace Rozzer.Tools.Models;

public class CallerNode(string signature) : CallableNode(signature), IComparable<CallerNode>
{
    public List<SymbolLocation> CallSites { get; } = [];

    public int CompareTo(CallerNode? other) => base.CompareTo(other);
}