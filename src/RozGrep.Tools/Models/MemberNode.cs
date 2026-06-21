namespace RozGrep.Tools.Models;

public class MemberNode(string signature) : CallableNode(signature), IComparable<MemberNode>
{
    public int CompareTo(MemberNode? other) => base.CompareTo(other);
}