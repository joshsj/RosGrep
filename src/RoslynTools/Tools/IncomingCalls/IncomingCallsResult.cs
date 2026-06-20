using System.Diagnostics.CodeAnalysis;

namespace RoslynTools.Tools.IncomingCalls;

public class IncomingCallsResult
{
    [MemberNotNullWhen(true, nameof(Report))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool IsSuccess { get; private init; }

    public IncomingCallsReport? Report { get; private init; }

    public string? Message { get; private init; }

    public static IncomingCallsResult Success(IncomingCallsReport report)
        => new() { IsSuccess = true, Report = report, };

    public static IncomingCallsResult Fail(string message)
        => new() { IsSuccess = false, Message = message, };
}