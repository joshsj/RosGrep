namespace Rozzer.Tools.IncomingCalls;

public record IncomingCallsToolOptions
{
    public required string SolutionName { get; init; }

    public required string TypeName { get; init; }
    
    public required int Depth { get; init; }
}