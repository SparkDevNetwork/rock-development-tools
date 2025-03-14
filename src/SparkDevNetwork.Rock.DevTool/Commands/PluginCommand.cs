using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

namespace SparkDevNetwork.Rock.DevTool.Commands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class PluginCommand : Command
{
    /// <summary>
    /// Creates a command that will handle routing for all plugin sub-commands.
    /// </summary>
    public PluginCommand( IServiceProvider serviceProvider )
        : base( "plugin", "Run commands related to working with plugins." )
    {
        AddCommand( ActivatorUtilities.CreateInstance<NewCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<ConfigureCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<PackCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<ImportCommand>( serviceProvider ) );
    }
}
