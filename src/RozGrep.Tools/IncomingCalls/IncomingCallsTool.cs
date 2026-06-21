using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using RozGrep.Tools.Definitions;
using RozGrep.Tools.Models;
using RozGrep.Tools.MSBuild;

namespace RozGrep.Tools.IncomingCalls;

public class IncomingCallsTool(
    ILogger<IncomingCallsTool> logger,
    MSBuildLocatorInitializer msBuildLocatorInitializer
)
{
    public async Task<IncomingCallsResult> InvokeAsync(IncomingCallsToolOptions options)
    {
        msBuildLocatorInitializer.EnsureInitialized();

        using var workspace = CreateWorkspace();

        var solution = await OpenSolution(options, workspace);

        var targetSymbol = await FindTargetSymbol(options, solution);

        if (targetSymbol is null)
        {
            return IncomingCallsResult.Fail($"Failed to find type '{options.SymbolName}'");
        }

        var memberSymbols = FindMemberSymbols(options, targetSymbol);

        if (memberSymbols is null)
        {
            return IncomingCallsResult.Fail($"No members found on '{options.SymbolName}'");
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
        logger.LogDebug("Looking for type '{TypeName}'", options.SymbolName);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                continue;
            }

            var candidates = compilation.GetSymbolsWithName(options.SymbolName, SymbolFilter.Type).OfType<INamedTypeSymbol>();

            if (options.SymbolType is not IncomingCallsToolSymbolType.Any)
            {
                var filteredTypeKind = options.SymbolType switch { 
                    IncomingCallsToolSymbolType.Interface => TypeKind.Interface,
                    IncomingCallsToolSymbolType.Class => TypeKind.Class,
                    IncomingCallsToolSymbolType.Struct => TypeKind.Struct,
                    _ => throw new UnreachableException()
                };

                candidates = candidates.Where(x => x.TypeKind == filteredTypeKind);
            }

            if (!string.IsNullOrWhiteSpace(options.SymbolNamespace))
            {
                // todo not sure if .Name is right
                // todo do I need to filter by NamespaceKind
                candidates = candidates.Where(x => x.ContainingNamespace.Name == options.SymbolNamespace);
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
        // todo make configurable
        var members = symbol.GetMembers().Where(m => m.Kind is SymbolKind.Method or SymbolKind.Property);

        if (options.IncludedMembers.Count > 0)
        {
            members = members.Where(x => options.IncludedMembers.Contains(x.Name));
        }
        
        if (options.ExcludedMembers.Count > 0)
        {
            members = members.Where(x => !options.ExcludedMembers.Contains(x.Name));
        }

        if (!members.Any())
        {
            logger.LogError(
                "No members found on '{InterfaceName}' with filters included='{Included}', excluded='{Excluded}'",
                options.SymbolName,
                string.Join(", ", options.IncludedMembers),
                string.Join(", ", options.ExcludedMembers)
            );

            return null;
        }

        return members;
    }
}