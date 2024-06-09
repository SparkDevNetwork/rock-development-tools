using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

/// <summary>
/// The command to update the plugins and the Rock binary distribution to match
/// what is configured in the environment.
/// </summary>
class UpdateCommand : Command
{
    /// <summary>
    /// Creates a command that will handle updating the environment.
    /// </summary>
    public UpdateCommand( IServiceProvider serviceProvider )
        : base( "update", "Updates the environment." )
    {
        AddCommand( ActivatorUtilities.CreateInstance<UpdateCommands.RockCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<UpdateCommands.PluginCommand>( serviceProvider ) );
    }
}
