namespace RoslynTools.Tools.IncomingCalls;

public class MemberNode
{
    public string Signature { get; set; } = "";

    public List<CallerNode> Callers { get; set; } = new();
}