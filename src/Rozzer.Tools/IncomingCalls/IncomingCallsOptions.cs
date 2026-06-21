namespace Rozzer.Tools.IncomingCalls;

public record IncomingCallsToolOptions
{
    public required string WorkspaceName { get; init; }

    public required string SymbolName { get; init; }

    public required IncomingCallsToolSymbolType SymbolType { get; init; }
    
    public required int Depth { get; init; }
}