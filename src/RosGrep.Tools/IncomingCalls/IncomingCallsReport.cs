using RosGrep.Tools.Models;

namespace RosGrep.Tools.IncomingCalls;

public class IncomingCallsReport(string typeName)
{
    public string TypeName { get; init; } = typeName;

    public List<MemberNode> Members { get; } = [];
}