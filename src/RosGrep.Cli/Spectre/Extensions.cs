using Spectre.Console.Cli;

namespace RosGrep.Cli.Spectre;

internal static class Extensions
{
    extension(IConfigurator configurator)
    {
        public ICommandConfigurator AddCommand<TCommand, TArgs>(string name, IServiceProvider serviceProvider)
            where TCommand : Commands.ICommand<TArgs>
            where TArgs : CommandSettings
            => configurator.AddAsyncDelegate(name, SpectreCommandAdapter.CreateDelegate<TCommand, TArgs>(serviceProvider));
    }
}