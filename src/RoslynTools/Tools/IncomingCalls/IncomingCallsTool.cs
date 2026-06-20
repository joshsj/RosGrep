using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using RoslynTools.MSBuild;

namespace RoslynTools.Tools.IncomingCalls;

public class IncomingCallsTool(
    ILogger<IncomingCallsTool> logger,
    MSBuildLocatorInitializer msBuildLocatorInitializer
)
{
    // todo make configurable (or just get rid)
    private static readonly string[] Ignored =
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

        var interfaceSymbol = await FindInterfaceSymbol(options, solution);

        if (interfaceSymbol is null)
        {
            return IncomingCallsResult.Fail($"Failed to find type '{options.TypeName}'");
        }

        var members = FindMembers(options, interfaceSymbol);

        if (members is null)
        {
            return IncomingCallsResult.Fail($"No members found on '{options.TypeName}'");
        }

        var callerFinder = new CallerFinder(solution, options.Depth);

        foreach (var member in members)
        {
            // todo need to put this back somewhere
            // .OrderBy(p => p.Key.ContainingType?.Name ?? "").ThenBy(p => p.Key.Name)
            await callerFinder.FindCallsAsync(member);
        }

        var report = new IncomingCallsReport(interfaceSymbol.ToDisplayString(), callerFinder.Members);

        return IncomingCallsResult.Success(report);
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();

        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure &&
                Ignored.All(x => !args.Diagnostic.Message.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError("{DiagnosticMessage}", args.Diagnostic.Message);
            }
        });

        return workspace;
    }

    private async Task<Solution> OpenSolution(IncomingCallsToolOptions options, MSBuildWorkspace workspace)
    {
        logger.LogDebug("Opening solution {SolutionName} ...", options.SolutionName);

        var solution = options.SolutionName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? (await workspace.OpenProjectAsync(options.SolutionName)).Solution
            : (await workspace.OpenSolutionAsync(options.SolutionName)).Workspace.CurrentSolution;

        logger.LogDebug("Opened solution, {Count} project(s) total", solution.Projects.Count());

        return solution;
    }

    private async Task<INamedTypeSymbol?> FindInterfaceSymbol(IncomingCallsToolOptions options, Solution solution)
    {
        logger.LogDebug("Looking for type '{TypeName}'", options.TypeName);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();

            var candidate = compilation?.GetSymbolsWithName(options.TypeName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                // todo doesn't have to be an interface
                // todo first or default is pants here, don't assume 1
                .FirstOrDefault(t => t.TypeKind == TypeKind.Interface);

            if (candidate is not null)
            {
                logger.LogDebug("Found type '{TypeName}'", candidate.Name);

                return candidate;
            }
        }

        logger.LogError("Failed to find type '{TypeName}'", options.TypeName);
        return null;
    }

    private List<ISymbol>? FindMembers(IncomingCallsToolOptions options, INamedTypeSymbol symbol)
    {
        // todo make configurable
        var members = symbol.GetMembers().Where(m => m.Kind is SymbolKind.Method or SymbolKind.Property);

        var hasWhitelist = options.TypeMemberNames is not null;

        if (hasWhitelist)
        {
            members = members.Where(x => options.TypeMemberNames!.Contains(x.Name));
        }

        var allocated = members.OrderBy(m => m.Name).ToList();

        if (allocated.Count is 0)
        {
            logger.LogError("No members found on '{InterfaceName}'", options.TypeName);

            return null;
        }

        return allocated;
    }
}