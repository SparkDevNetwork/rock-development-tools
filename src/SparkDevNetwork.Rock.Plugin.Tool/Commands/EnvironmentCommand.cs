using System.CommandLine;

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
    }
}
