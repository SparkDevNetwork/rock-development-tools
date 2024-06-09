using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

namespace SparkDevNetwork.Rock.DevTool.Commands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class EnvironmentCommand : Command
{
    /// <summary>
    /// Creates a command that will handle routing for all development
    /// environment sub-commands.
    /// </summary>
    public EnvironmentCommand( IServiceProvider serviceProvider )
        : base( "environment", "Run commands related to working with development environments." )
    {
        AddAlias( "env" );

        AddCommand( ActivatorUtilities.CreateInstance<NewCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<UpdateCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<StatusCommand>( serviceProvider ) );
    }
}
