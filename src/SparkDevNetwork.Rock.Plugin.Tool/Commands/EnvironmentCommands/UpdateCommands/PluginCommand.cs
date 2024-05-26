using System.CommandLine;
using System.CommandLine.Invocation;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the plugins to match what is configured in the
/// environment.
/// </summary>
class PluginCommand : Abstractions.BaseModifyCommand<PluginCommandOptions, PluginCommandHandler>
{
    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    /// <summary>
    /// Creates a command that will handle updating the Rock installation.
    /// </summary>
    public PluginCommand()
        : base( "plugin", "Updates the plugins to match the configuration file." )
    {
        _targetOption = new Option<string?>( "--target", "The directory that contains the environment." );
        _targetOption.AddAlias( "-t" );

        AddOption( _targetOption );
    }

    /// <inheritdoc/>
    protected override PluginCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Target = context.ParseResult.GetValueForOption( _targetOption );

        return options;
    }
}
