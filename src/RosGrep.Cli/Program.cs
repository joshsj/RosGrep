using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RosGrep.Cli.Commands;
using RozGrep.Tools;
using RosGrep.Cli.Commands.IncomingCalls;
using RosGrep.Cli.Spectre;
using Spectre.Console.Cli;

var services = new ServiceCollection();

services.AddLogging(x => x.ClearProviders().AddConsole());

services.RegisterRosGrep();

services.AddTransient(typeof(CommandHandler<,>));

foreach (var type in ScanForCommandTypes())
{
    services.AddTransient(type);
}

var serviceProvider = services.BuildServiceProvider();

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("RosGrep");

    config.AddCommand<IncomingCallsCommand, IncomingCallsArgs>("incoming-calls", serviceProvider)
        .WithDescription("Find calls recursively up the call stack.");
});

return await app.RunAsync(args);

IEnumerable<Type> ScanForCommandTypes()
{
    // Blimey
    static bool IsCommand(Type t) =>
        t is { IsClass: true, IsAbstract: false } &&
        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRosGrepCommand<>));

    return Assembly.GetExecutingAssembly() .GetTypes() .Where(IsCommand); }
