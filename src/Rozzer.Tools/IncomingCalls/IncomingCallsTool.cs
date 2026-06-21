using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Rozzer.Tools.Definitions;
using Rozzer.Tools.Models;
using Rozzer.Tools.MSBuild;

namespace Rozzer.Tools.IncomingCalls;

public class IncomingCallsTool(
    ILogger<IncomingCallsTool> logger,
    MSBuildLocatorInitializer msBuildLocatorInitializer
)
{
    // todo make configurable (or just get rid)
    private static readonly string[] IgnoredMsBuildErrors =
    [
        "may not be fully compatible with your project",
        "detected package downgrade",
        "detected package version outside of dependency constraint",
        "severity vulnerability",
    ];

    public async Task<IncomingCallsResult> InvokeAsync(IncomingCallsToolOptions options)
    {
        msBuildLocatorInitializer.EnsureInitialized();

        using var workspace = CreateWorkspace();

        var solution = await OpenSolution(options, workspace);

        var typeNameSymbol = await FindTypeNameSymbol(options, solution);

        if (typeNameSymbol is null)
        {
            return IncomingCallsResult.Fail($"Failed to find type '{options.TypeName}'");
        }

        var memberSymbols = FindMemberSymbols(options, typeNameSymbol);

        if (memberSymbols is null)
        {
            return IncomingCallsResult.Fail($"No members found on '{options.TypeName}'");
        }

        var callerFinder = new CallerFinder(solution, options.Depth);
        var report = new IncomingCallsReport(typeNameSymbol.ToDisplayString());

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
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure &&
                IgnoredMsBuildErrors.All(x => !args.Diagnostic.Message.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError("{DiagnosticMessage}", args.Diagnostic.Message);
            }
        });

        return workspace;
    }

    private async Task<Solution> OpenSolution(IncomingCallsToolOptions options, MSBuildWorkspace workspace)
    {
        logger.LogDebug("Opening solution {SolutionName} ...", options.SolutionName);

        // todo allow just folder name to be specified? would be nice if we can reuse the logic of dotnet cli to find the "single" target
        var solution = options.SolutionName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? (await workspace.OpenProjectAsync(options.SolutionName)).Solution
            : (await workspace.OpenSolutionAsync(options.SolutionName)).Workspace.CurrentSolution;

        logger.LogDebug("Opened solution, {Count} project(s) total", solution.Projects.Count());

        return solution;
    }

    private async Task<INamedTypeSymbol?> FindTypeNameSymbol(IncomingCallsToolOptions options, Solution solution)
    {
        logger.LogDebug("Looking for type '{TypeName}'", options.TypeName);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();

            var candidate = compilation?.GetSymbolsWithName(options.TypeName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(t => t.TypeKind is TypeKind.Interface or TypeKind.Class or TypeKind.Struct);

            if (candidate is not null)
            {
                logger.LogDebug("Found type '{TypeName}'", candidate.Name);

                return candidate;
            }
        }

        logger.LogError("Failed to find type '{TypeName}'", options.TypeName);
        return null;
    }

    private List<ISymbol>? FindMemberSymbols(IncomingCallsToolOptions options, INamedTypeSymbol symbol)
    {
        // todo make configurable
        var allocated = symbol.GetMembers()
            .Where(m => m.Kind is SymbolKind.Method or SymbolKind.Property)
            .OrderBy(m => m.Name)
            .ToList();

        if (allocated.Count is 0)
        {
            logger.LogError("No members found on '{InterfaceName}'", options.TypeName);

            return null;
        }

        return allocated;
    }
}