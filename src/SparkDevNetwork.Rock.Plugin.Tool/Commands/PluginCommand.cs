using System.CommandLine;

using SparkDevNetwork.Rock.Plugin.Tool.Commands.PluginCommands;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class PluginCommand : Command
{
    /// <summary>
    /// Creates a command that will handle routing for all plugin sub-commands.
    /// </summary>
    public PluginCommand()
        : base( "plugin", "Run commands related to working with plugins." )
    {
        AddCommand( new CreateCommand() );
    }
}
