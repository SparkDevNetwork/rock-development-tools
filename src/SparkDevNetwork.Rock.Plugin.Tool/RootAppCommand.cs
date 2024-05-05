using System.CommandLine;

using SparkDevNetwork.Rock.Plugin.Tool.Commands;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// The root command handler to process the command line arguments.
/// </summary>
class RootAppCommand : RootCommand
{
    /// <summary>
    /// Creates a new command handler.
    /// </summary>
    public RootAppCommand()
        : base( "Plugin development tool for RockRMS." )
    {
        // Name = "rock-plugin-tool";

        AddCommand( new EnvironmentCommand() );
        AddCommand( new PluginCommand() );
    }
}
