using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoslynTools;
using RoslynTools.Cli.Commands;
using RoslynTools.Cli.Commands.Results;

using var parser = new Parser(settings =>
{
    settings.CaseInsensitiveEnumValues = true; // accept --format json as well as Json
    settings.HelpWriter = Console.Error; // keep stdout clean for the report
});

var commandAndOptionTypes = FindCommandAndOptionTypes().ToList();

var parsed = parser.ParseArguments(args, commandAndOptionTypes.Select(x => x.OptionType).ToArray());

if (parsed.Tag == ParserResultType.NotParsed)
{
    // todo better
    return 1;
}

var services = new ServiceCollection();

services.RegisterRoslynTools();

foreach (var (commandType, _) in commandAndOptionTypes)
{
    services.AddTransient(commandType);
}

services.AddLogging(x => x.ClearProviders().AddConsole());

var serviceProvider = services.BuildServiceProvider();

var isError = false;

await parsed.WithParsedAsync(async options =>
{
    var commandType = options.GetType().DeclaringType!;
    
    await using var serviceScope = serviceProvider.CreateAsyncScope();

    var command = (ICommand)serviceScope.ServiceProvider.GetRequiredService(commandType);

    var result = await command.ExecuteAsync(options);
    
    switch (result) {
        case LogResult logResult:
        {
            // todo or use logger?
            Console.WriteLine(logResult.Message);
            break;
        }

        case ErrorResult errorResult:
        {
            serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogError("{Message}", errorResult.Message);
            isError = true;
            break;
        }
    }
});

return isError ? 1 : 0;

IEnumerable<(Type CommandType, Type OptionType)> FindCommandAndOptionTypes() =>
    Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(x => x.GetCustomAttribute<VerbAttribute>() is not null)
        .Select(x => (x.DeclaringType!, x));
 