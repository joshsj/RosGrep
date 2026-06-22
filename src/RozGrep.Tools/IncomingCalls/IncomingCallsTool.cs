using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using RozGrep.Tools.Definitions;
using RozGrep.Tools.Models;
using RozGrep.Tools.MSBuild;

namespace RozGrep.Tools.IncomingCalls;

using TargetTypeKind = IncomingCallsToolTargetTypeKind;
using MemberSymbolKind = IncomingCallsToolMemberSymbolKind;

public class IncomingCallsTool(
    ILogger<IncomingCallsTool> logger,
    MSBuildLocatorInitializer msBuildLocatorInitializer
)
{
    private static IReadOnlyDictionary<TargetTypeKind, TypeKind> TargetTypeKindMap { get; } =
        new Dictionary<TargetTypeKind, TypeKind>
        {
            [TargetTypeKind.Interface] = TypeKind.Interface,
            [TargetTypeKind.Class] = TypeKind.Class,
            [TargetTypeKind.Struct] = TypeKind.Struct,
        };
    
    private static IReadOnlySet<TypeKind> SupportedTargetTypeKinds { get; } = TargetTypeKindMap.Values.ToHashSet();

    private static IReadOnlyDictionary<MemberSymbolKind, SymbolKind> MemberSymbolKindMap { get; } =
        new Dictionary<MemberSymbolKind, SymbolKind>
        {
            [MemberSymbolKind.Method] = SymbolKind.Method,
            [MemberSymbolKind.Property] = SymbolKind.Property,
        };

    private static IReadOnlySet<SymbolKind> SupportedMemberSymbolKinds { get; } = MemberSymbolKindMap.Values.ToHashSet();

    public async Task<IncomingCallsResult> InvokeAsync(IncomingCallsToolOptions options)
    {
        msBuildLocatorInitializer.EnsureInitialized();

        using var workspace = CreateWorkspace();

        var solution = await OpenSolution(options, workspace);

        var targetSymbol = await FindTargetSymbol(options, solution);

        if (targetSymbol is null)
        {
            return IncomingCallsResult.Fail($"Failed to find type '{options.TargetName}'");
        }

        var memberSymbols = FindMemberSymbols(options, targetSymbol);

        if (memberSymbols is null)
        {
            return IncomingCallsResult.Fail($"No members found on '{options.TargetName}'");
        }

        var callerFinder = new CallerFinder(solution, options.Depth);
        var report = new IncomingCallsReport(targetSymbol.ToDisplayString());

        foreach (var memberSymbol in memberSymbols)
        {
            var memberNode = new MemberNode(memberSymbol.ToDisplayString(Constants.Formatting.MemberDisplayFormat));

            memberNode.Definitions.AddRange(
                memberSymbol.Locations.Where(x => x.IsInSource).Select(SymbolLocation.From).Order()
            );

            await foreach (var callerNode in callerFinder.FindCallsAsync(memberSymbol))
            {
                memberNode.Callers.Add(callerNode);
            }

            report.Members.Add(memberNode);
        }

        report.Members.Sort();

        return IncomingCallsResult.Success(report);
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();

        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                logger.LogError("{DiagnosticMessage}", args.Diagnostic.Message);
            }
        });

        return workspace;
    }

    private async Task<Solution> OpenSolution(IncomingCallsToolOptions options, MSBuildWorkspace workspace)
    {
        logger.LogDebug("Opening solution {SolutionName} ...", options.WorkspaceName);

        // todo allow just folder name to be specified? would be nice if we can reuse the logic of dotnet cli to find the "single" target
        var solution = options.WorkspaceName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? (await workspace.OpenProjectAsync(options.WorkspaceName)).Solution
            : (await workspace.OpenSolutionAsync(options.WorkspaceName)).Workspace.CurrentSolution;

        logger.LogDebug("Opened solution, {Count} project(s) total", solution.Projects.Count());

        return solution;
    }

    private async Task<INamedTypeSymbol?> FindTargetSymbol(IncomingCallsToolOptions options, Solution solution)
    {
        logger.LogDebug("Looking for type '{TypeName}'", options.TargetName);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                continue;
            }

            var candidates = compilation.GetSymbolsWithName(options.TargetName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>();

            if (options.TargetTypeKind is not null)
            {
                var filtered = TargetTypeKindMap[options.TargetTypeKind.Value];
                candidates = candidates.Where(x => x.TypeKind == filtered);
            }
            else
            {
                candidates = candidates.Where(x => SupportedTargetTypeKinds.Contains(x.TypeKind));
            }

            if (!string.IsNullOrWhiteSpace(options.TargetNamespace))
            {
                // todo not sure if .Name is right
                // todo do I need to filter by NamespaceKind
                candidates = candidates.Where(x => x.ContainingNamespace.Name == options.TargetNamespace);
            }

            var matches = candidates.ToList();

            switch (matches.Count)
            {
                case 0:
                {
                    logger.LogDebug("Type not found in project '{ProjectName}'", project.Name);
                    continue;
                }
                case 1:
                {
                    logger.LogDebug("Type found in project '{ProjectName}'", project.Name);
                    return matches[0];
                }
                default:
                {
                    logger.LogDebug(
                        "Multiple matching types found in project '{ProjectName}': {MatchingTypes}",
                        project.Name,
                        string.Join(", ", matches.Select(x => x.ToDisplayString()))
                    );
                    // todo report a better error in the return value
                    return null;
                }
            }
        }

        logger.LogError("Type not found in any project");
        return null;
    }

    private IEnumerable<ISymbol>? FindMemberSymbols(IncomingCallsToolOptions options, INamedTypeSymbol symbol)
    {
        IEnumerable<ISymbol> members = symbol
            .GetMembers()
            // todo hardcoded but seems sensible
            .Where(x => x is not IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet });

        if (options.IncludedMembers.Count > 0)
        {
            members = members.Where(x => options.IncludedMembers.Contains(x.Name));
        }

        if (options.ExcludedMembers.Count > 0)
        {
            members = members.Where(x => !options.ExcludedMembers.Contains(x.Name));
        }

        {
            var filtered = options.MemberSymbolKinds.Count > 0
                ? options.MemberSymbolKinds.Select(x => MemberSymbolKindMap[x]).ToHashSet()
                : SupportedMemberSymbolKinds;
 
            members = members.Where(x => filtered.Contains(x.Kind));
        }

        if (!members.Any())
        {
            logger.LogError(
                "No members found on '{InterfaceName}' with filters included='{Included}', excluded='{Excluded}'",
                options.TargetName,
                string.Join(", ", options.IncludedMembers),
                string.Join(", ", options.ExcludedMembers)
            );

            return null;
        }

        return members;
    }
}