using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class StatusCommand : Abstractions.BaseActionCommand<StatusCommandOptions, StatusCommandHandler>
{
    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    /// <summary>
    /// Creates a command that will handle checking on the status of an
    /// existing environment.
    /// </summary>
    public StatusCommand()
        : base( "status", "Checks the status of the environment against the configured values." )
    {
        _targetOption = new Option<string?>( "--target", "The directory that contains the environment." );
        _targetOption.AddAlias( "-t" );

        AddOption( _targetOption );
    }

    /// <inheritdoc/>
    protected override StatusCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Target = context.ParseResult.GetValueForOption( _targetOption );

        return options;
    }
}
