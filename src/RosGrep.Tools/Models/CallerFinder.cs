using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace RosGrep.Tools.Models;

internal class CallerFinder
{
    private readonly Solution _solution;
    private readonly int _maxDepth;

    internal CallerFinder(Solution solution, int maxDepth)
    {
        _solution = solution;
        _maxDepth = maxDepth;
    }

    internal IAsyncEnumerable<CallerNode> FindCallsAsync(ISymbol memberSymbol)
        => FindCallsAsync(memberSymbol, 0, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

    // Build the caller nodes for `symbol`, recursing. Merges call sites across the
    // symbol and every interface member it implements (DI dispatch).
    private async IAsyncEnumerable<CallerNode> FindCallsAsync(ISymbol memberSymbol, int currentDepth, HashSet<ISymbol> visited)
    {
        if (currentDepth >= _maxDepth || visited.Contains(memberSymbol))
        {
            yield break;
        }

        // Search callers of the symbol AND of any interface member it implements.
        var callSitesByCallerSymbol = new Dictionary<ISymbol, HashSet<SymbolLocation>>(SymbolEqualityComparer.Default);

        foreach (var target in GetSearchTargets(memberSymbol))
        {
            foreach (var symbolCallerInfo in await SymbolFinder.FindCallersAsync(target, _solution))
            {
                // todo make configurable so you can filter out calls to yourself
                // if (!symbolCallerInfo.IsDirect)
                // {
                //     continue;
                // }

                var callingSymbol = symbolCallerInfo.CallingSymbol;

                if (!callSitesByCallerSymbol.TryGetValue(callingSymbol, out var callSites))
                {
                    callSitesByCallerSymbol[callingSymbol] = callSites = [];
                }

                // Exclude callers not in source code
                foreach (var callingLocation in symbolCallerInfo.Locations.Where(x => x.IsInSource))
                {
                    callSites.Add(SymbolLocation.From(callingLocation));
                }
            }
        }

        foreach (var (callingSymbol, callSites) in callSitesByCallerSymbol)
        {
            visited.Add(callingSymbol);

            var node = new CallerNode(callingSymbol.ToDisplayString(Formatting.TypeAndMemberDisplayFormat));

            node.Definitions.AddRange(
                callingSymbol.Locations.Where(x => x.IsInSource).Select(SymbolLocation.From).Order()
            );

            node.CallSites.AddRange(callSites.Order());

            await foreach (var callerNode in FindCallsAsync(callingSymbol, currentDepth++, visited))
            {
                node.Callers.AddRange(callerNode);
            }

            node.Callers.Sort();

            yield return node;
        }
    }

    // The symbol itself plus every interface member it implements (implicit or explicit).
    // Callers that dispatch through the interface are only found by searching the interface member.
    private static IEnumerable<ISymbol> GetSearchTargets(ISymbol memberSymbol)
    {
        yield return memberSymbol;

        var containingType = memberSymbol.ContainingType;

        if (containingType is null)
        {
            yield break;
        }
        
        // todo make configurable (--include-base-members or something)
        // todo how does this work for virtual/override
        var memberSymbolsOfImplementedInterfaces =
            from @interface in containingType.AllInterfaces
            from member in @interface.GetMembers()
            let impl = containingType.FindImplementationForInterfaceMember(member)
            where impl is not null && SymbolEqualityComparer.Default.Equals(impl, memberSymbol)
            select member;

        foreach (var member in memberSymbolsOfImplementedInterfaces)
        {
            yield return member;
        }
    }
}