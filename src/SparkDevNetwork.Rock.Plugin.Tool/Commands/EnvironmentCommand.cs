using System.CommandLine;

using SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class EnvironmentCommand : Command
{
    /// <summary>
    /// Creates a command that will handle routing for all development
    /// environment sub-commands.
    /// </summary>
    public EnvironmentCommand()
        : base( "environment", "Run commands related to working with development environments." )
    {
        AddAlias( "env" );

        AddCommand( new NewCommand() );
        AddCommand( new UpdateCommand() );
        AddCommand( new StatusCommand() );
    }
}
