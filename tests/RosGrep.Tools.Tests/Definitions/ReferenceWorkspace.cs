namespace RosGrep.Tools.Tests.Definitions;

public static class ReferenceWorkspace
{
    public static string Directory => Path.Join(TestContext.OutputDirectory, "../../../../reference-solution");

    public static string Slnx => Path.Join(Directory, "ReferenceSolution.slnx");
}