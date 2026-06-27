namespace RosGrep.Tools.Tests.Definitions;

public static class ReferenceWorkspace
{
    public static string Directory => Path.Join(TestContext.OutputDirectory, "../../../../reference-workspace");

    public static string Slnx => Path.Join(Directory, "ReferenceWorkspace.slnx");
}