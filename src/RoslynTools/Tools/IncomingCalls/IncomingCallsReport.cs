namespace RoslynTools.Tools.IncomingCalls;

public record IncomingCallsReport(string TypeName)
{
    public List<MemberNode> Members { get; } = [];
    
    public IncomingCallsReport(string typeName, IEnumerable<MemberNode> members) : this(typeName)
    {
        Members.AddRange(members);
    }
}