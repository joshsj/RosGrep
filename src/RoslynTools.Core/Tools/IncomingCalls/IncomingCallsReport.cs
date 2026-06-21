namespace RoslynTools.Core.Tools.IncomingCalls;

public class IncomingCallsReport(string typeName)
{
    public string TypeName { get; init; } = typeName;

    public List<MemberNode> Members { get; } = [];
}