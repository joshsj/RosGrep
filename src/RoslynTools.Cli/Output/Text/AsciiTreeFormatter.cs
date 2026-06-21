using System.Text;

namespace RoslynTools.Cli.Output.Text;

public static class AsciiTreeFormatter
{
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";

    public static string Format(AsciiTreeNode root)
    {
        var sb = new StringBuilder();

        PrintNode(sb, root, "");

        return sb.ToString();
    }

    private static void PrintNode(StringBuilder sb, AsciiTreeNode node, string indent)
    {
        sb.Append(' ');
        sb.AppendLine(node.Name);

        var childCount = node.Children.Count();

        foreach (var (i, child) in node.Children.Index())
        {
            sb.Append(indent);

            if (i == childCount - 1)
            {
                sb.Append(Corner);
                PrintNode(sb, child, indent + Space);
            }
            else
            {
                sb.Append(Cross);
                PrintNode(sb, child, indent + Vertical);
            }
        }
    }
}

public readonly record struct AsciiTreeNode(string Name, IEnumerable<AsciiTreeNode> Children);