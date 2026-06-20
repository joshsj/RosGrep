using Microsoft.CodeAnalysis;

namespace RoslynTools.Tools.IncomingCalls;

public readonly record struct CallSite(string File, int Line, int Character)
{
    internal static CallSite From(Location location)
    {
        var span = location.GetLineSpan();

        return new CallSite(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
    }
}