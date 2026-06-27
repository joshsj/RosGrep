using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RosGrep.Tools.IncomingCalls;

namespace RosGrep.Tools.Tests.IncomingCalls;

public class ClassTests
{
    private ServiceProvider _serviceProvider = null!;

    private Task<IncomingCallsResult> Act(IncomingCallsToolArgs args)
        => _serviceProvider.GetRequiredService<IncomingCallsTool>().InvokeAsync(args, CancellationToken.None);

    [Before(Test)]
    public void Setup()
    {
        var services = new ServiceCollection();

        services
            // todo will do for now, stub is probs better though
            .AddLogging(x => x.ClearProviders())
            .RegisterRosGrep();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public async Task Test1()
    {
        var args = new IncomingCallsToolArgs
        {
            WorkspaceName = ReferenceWorkspace.Slnx,
            TargetName = "Class",
            Depth = 100,
        };

        var result = await Act(args);

        // todo finish
        await Assert.That(result)
            .Member(x => x.IsSuccess, x => x.IsTrue())
            .And
            .Member(x => x.Report!.TypeName, x => x.IsEqualTo("ProjectA.Class"));
    }
}