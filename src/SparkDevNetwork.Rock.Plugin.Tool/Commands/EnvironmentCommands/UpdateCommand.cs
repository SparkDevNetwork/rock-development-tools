using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// The command to update the plugins and the Rock binary distribution to match
/// what is configured in the environment.
/// </summary>
class UpdateCommand : Abstractions.BaseModifyCommand<UpdateCommandOptions, UpdateCommandHandler>
{
    private readonly Option<string?> _targetOption;

    /// <summary>
    /// Creates a command that will handle updating the environment.
    /// </summary>
    public UpdateCommand()
        : base( "update", "Updates the environment to match the configuration file." )
    {
        _targetOption = new Option<string?>( "--target", "The directory that contains the environment." );
        _targetOption.AddAlias( "-t" );

        AddOption( _targetOption );
    }

    /// <inheritdoc/>
    protected override UpdateCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Target = context.ParseResult.GetValueForOption( _targetOption );

        return options;
    }
}
