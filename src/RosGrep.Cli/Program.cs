using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RozGrep.Tools;
using RosGrep.Cli.Commands;

using var parser = new Parser(settings =>
{
    settings.CaseInsensitiveEnumValues = true; // accept --format json as well as Json
    settings.HelpWriter = Console.Error; // keep stdout clean for the report
});

var commandAndOptionTypes = FindCommandAndOptionTypes().ToList();

var parsed = parser.ParseArguments(args, commandAndOptionTypes.Select(x => x.OptionType).ToArray());

if (parsed.Tag == ParserResultType.NotParsed)
{
    return 1;
}

var services = new ServiceCollection();

services.RegisterRosGrep();

services.AddTransient<CommandExecutor>();

foreach (var (type, _) in commandAndOptionTypes)
{
    services.AddTransient(type);
}

services.AddLogging(x => x.ClearProviders().AddConsole());

var serviceProvider = services.BuildServiceProvider();

var options = parsed.Value;
var commandType = options.GetType().DeclaringType!;
    
await using var serviceScope = serviceProvider.CreateAsyncScope();

var commandExecutor = serviceScope.ServiceProvider.GetRequiredService<CommandExecutor>();

return await commandExecutor.ExecuteAsync(commandType, options);

IEnumerable<(Type CommandType, Type OptionType)> FindCommandAndOptionTypes() =>
    Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(x => x.GetCustomAttribute<VerbAttribute>() is not null)
        .Select(x => (x.DeclaringType!, x));
 