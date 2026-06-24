using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
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

    public async Task<IncomingCallsResult> InvokeAsync(IncomingCallsToolArgs args, CancellationToken cancellationToken)
    {
        msBuildLocatorInitializer.EnsureInitialized();

        using var workspace = CreateWorkspace();

        var solution = await OpenSolution(args, workspace, cancellationToken);

        var targetSymbol = await FindTargetSymbol(args, solution, cancellationToken);

        if (targetSymbol is null)
        {
            return IncomingCallsResult.Fail($"Failed to find type '{args.TargetName}'");
        }

        var memberSymbols = FindMemberSymbols(args, targetSymbol);

        if (memberSymbols is null)
        {
            return IncomingCallsResult.Fail($"No members found on '{args.TargetName}'");
        }

        var callerFinder = new CallerFinder(solution, args.Depth);
        var report = new IncomingCallsReport(targetSymbol.ToDisplayString());

        foreach (var memberSymbol in memberSymbols)
        {
            var memberNode = new MemberNode(memberSymbol.ToDisplayString(Formatting.MemberDisplayFormat));

            memberNode.Definitions.AddRange(
                memberSymbol.Locations.Where(x => x.IsInSource).Select(SymbolLocation.From).Order()
            );

            await foreach (var callerNode in callerFinder.FindCallsAsync(memberSymbol).WithCancellation(cancellationToken))
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

    private async Task<Solution> OpenSolution(IncomingCallsToolArgs args, MSBuildWorkspace workspace, CancellationToken cancellationToken)
    {
        logger.LogDebug("Opening solution {SolutionName} ...", args.WorkspaceName);

        // todo allow just folder name to be specified? would be nice if we can reuse the logic of dotnet cli to find the "single" target
        var solution = args.WorkspaceName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? (await workspace.OpenProjectAsync(args.WorkspaceName, cancellationToken: cancellationToken)).Solution
            : (await workspace.OpenSolutionAsync(args.WorkspaceName, cancellationToken: cancellationToken)).Workspace.CurrentSolution;

        logger.LogDebug("Opened solution, {Count} project(s) total", solution.Projects.Count());

        return solution;
    }

    private async Task<INamedTypeSymbol?> FindTargetSymbol(IncomingCallsToolArgs args, Solution solution,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Looking for type '{TypeName}'", args.TargetName);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);

            if (compilation is null)
            {
                continue;
            }

            var candidates = compilation.GetSymbolsWithName(args.TargetName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>();

            if (args.TargetTypeKind is not null)
            {
                var filtered = TargetTypeKindMap[args.TargetTypeKind.Value];
                candidates = candidates.Where(x => x.TypeKind == filtered);
            }
            else
            {
                candidates = candidates.Where(x => SupportedTargetTypeKinds.Contains(x.TypeKind));
            }

            if (!string.IsNullOrWhiteSpace(args.TargetNamespace))
            {
                // todo not sure if .Name is right
                // todo do I need to filter by NamespaceKind
                candidates = candidates.Where(x => x.ContainingNamespace.Name == args.TargetNamespace);
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

    private IEnumerable<ISymbol>? FindMemberSymbols(IncomingCallsToolArgs args, INamedTypeSymbol symbol)
    {
        IEnumerable<ISymbol> members = symbol
            .GetMembers()
            // todo hardcoded but seems sensible
            .Where(x => x is not IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet });

        if (args.IncludedMembers.Count > 0)
        {
            members = members.Where(x => args.IncludedMembers.Contains(x.Name));
        }

        if (args.ExcludedMembers.Count > 0)
        {
            members = members.Where(x => !args.ExcludedMembers.Contains(x.Name));
        }

        {
            var filtered = args.MemberSymbolKinds.Count > 0
                ? args.MemberSymbolKinds.Select(x => MemberSymbolKindMap[x]).ToHashSet()
                : SupportedMemberSymbolKinds;
 
            members = members.Where(x => filtered.Contains(x.Kind));
        }

        if (!members.Any())
        {
            logger.LogError(
                "No members found on '{InterfaceName}' with filters included='{Included}', excluded='{Excluded}'",
                args.TargetName,
                string.Join(", ", args.IncludedMembers),
                string.Join(", ", args.ExcludedMembers)
            );

            return null;
        }

        return members;
    }
}