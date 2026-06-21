using Microsoft.CodeAnalysis;

namespace RoslynTools.Core.Tools.IncomingCalls;

public readonly record struct SymbolLocation(string File, int Line, int Character) : IComparable<SymbolLocation>
{
    internal static SymbolLocation From(Location location)
    {
        var span = location.GetLineSpan();

        return new SymbolLocation(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
    }

    public int CompareTo(SymbolLocation other)
    {
        var fileComparison = string.Compare(File, other.File, StringComparison.Ordinal);

        if (fileComparison != 0)
        {
            return fileComparison;
        }

        var lineComparison = Line.CompareTo(other.Line);

        if (lineComparison != 0)
        {
            return lineComparison;
        }

        return Character.CompareTo(other.Character);
    }
}