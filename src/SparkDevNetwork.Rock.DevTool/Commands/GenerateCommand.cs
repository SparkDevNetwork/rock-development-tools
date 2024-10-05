using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

namespace SparkDevNetwork.Rock.DevTool.Commands;

/// <summary>
/// Container for sub-commands related to working with code generation.
/// </summary>
class GenerateCommand : Command
{
    /// <summary>
    /// Creates a command that will handle routing for all plugin sub-commands.
    /// </summary>
    public GenerateCommand( IServiceProvider serviceProvider )
        : base( "generate", "Run commands related to code generation." )
    {
        AddCommand( ActivatorUtilities.CreateInstance<ViewModelsCommand>( serviceProvider ) );
    }
}
