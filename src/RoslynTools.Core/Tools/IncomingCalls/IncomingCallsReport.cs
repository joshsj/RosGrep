using System.Diagnostics.CodeAnalysis;

namespace RoslynTools.Core.Tools.IncomingCalls;

public class IncomingCallsReport
{
    public required string TypeName { get; init;  }

    public List<MemberNode> Members { get; } = [];
    
    [SetsRequiredMembers]
    public IncomingCallsReport(string typeName, IEnumerable<MemberNode> members)
    {
        TypeName = typeName;
        Members.AddRange(members);
    }
}