using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace RoslynTools.Tools.IncomingCalls;

internal class CallerFinder
{
    // todo is this more presentational?
    private static readonly SymbolDisplayFormat TypeAndMemberDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    
    private static readonly SymbolDisplayFormat MemberDisplayFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private readonly Solution _solution;
    private readonly int _maxDepth;

    public List<MemberNode> FoundMembers { get; } = [];

    internal CallerFinder(Solution solution, int maxDepth)
    {
        _solution = solution;
        _maxDepth = maxDepth;
    }

    internal async Task FindCallsAsync(ISymbol memberSymbol)
    {
        var callers = await FindCallsAsync(memberSymbol, 0, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
        var memberNode = new MemberNode(memberSymbol.ToDisplayString(MemberDisplayFormat), callers);

        FoundMembers.Add(memberNode);
    }

    // Build the caller nodes for `symbol`, recursing. Merges call sites across the
    // symbol and every interface member it implements (DI dispatch).
    private async Task<IEnumerable<CallerNode>> FindCallsAsync(ISymbol memberSymbol, int currentDepth, HashSet<ISymbol> visited)
    {
        // Search callers of the symbol AND of any interface member it implements.
        var callSitesByCallerSymbol = new Dictionary<ISymbol, HashSet<CallSite>>(SymbolEqualityComparer.Default);

        foreach (var target in GetSearchTargets(memberSymbol))
        {
            foreach (var symbolCallerInfo in await SymbolFinder.FindCallersAsync(target, _solution))
            {
                // todo make configurable so you can filter out calls to yourself
                // if (!info.IsDirect)
                // {
                //     continue;
                // }

                if (!callSitesByCallerSymbol.TryGetValue(symbolCallerInfo.CallingSymbol, out var callSites))
                {
                    callSitesByCallerSymbol[symbolCallerInfo.CallingSymbol] = callSites = [];
                }

                // Exclude callers not in source code
                foreach (var callerLocation in symbolCallerInfo.Locations.Where(x => x.IsInSource))
                {
                    callSites.Add(CallSite.From(callerLocation));
                }
            }
        }

        var nodes = new List<CallerNode>();
        foreach (var (caller, value) in callSitesByCallerSymbol)
        {
            var node = new CallerNode
            {
                Signature = caller.ToDisplayString(TypeAndMemberDisplayFormat),
            };

            node.CallSites.AddRange(value.OrderBy(x => x.File).ThenBy(x => x.Line));

            var nextDepth = ++currentDepth;
            
            if (nextDepth < _maxDepth)
            {
                visited.Add(caller);

                var callers = await FindCallsAsync(caller, nextDepth, visited);

                node.Callers.AddRange(callers);
            }

            nodes.Add(node);
        }

        return nodes.OrderBy(x => x.Signature);
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