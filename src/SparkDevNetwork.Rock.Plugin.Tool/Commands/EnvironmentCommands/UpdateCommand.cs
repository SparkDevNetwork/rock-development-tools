using System.CommandLine;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// The command to update the plugins and the Rock binary distribution to match
/// what is configured in the environment.
/// </summary>
class UpdateCommand : Command
{
    /// <summary>
    /// Creates a command that will handle updating the environment.
    /// </summary>
    public UpdateCommand()
        : base( "update", "Updates the environment." )
    {
        AddCommand( new UpdateCommands.RockCommand() );
        AddCommand( new UpdateCommands.PluginCommand() );
    }
}
