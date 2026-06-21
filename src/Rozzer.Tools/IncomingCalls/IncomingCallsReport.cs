using Rozzer.Tools.Models;

namespace Rozzer.Tools.IncomingCalls;

public class IncomingCallsReport(string typeName)
{
    public string TypeName { get; init; } = typeName;

    public List<MemberNode> Members { get; } = [];
}