using System.CommandLine;
using System.CommandLine.Invocation;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the plugins to match what is configured in the
/// environment.
/// </summary>
class PluginCommand : Abstractions.BaseModifyCommand<PluginCommandOptions>
{
    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a command that will handle updating the Rock installation.
    /// </summary>
    public PluginCommand( IServiceProvider serviceProvider )
        : base( "plugin", "Updates the plugins to match the configuration file.", serviceProvider )
    {
        _serviceProvider = serviceProvider;

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

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var environmentDirectory = ExecuteOptions.Target ?? Directory.GetCurrentDirectory();

        var environment = Environment.Open( environmentDirectory, _serviceProvider );

        if ( environment is null )
        {
            return Task.FromResult( 1 );
        }

        environment.IsDryRun = ExecuteOptions.DryRun;

        var plugins = environment.GetPlugins();
        var outOfDatePlugins = plugins
            .Where( p => !environment.GetPluginStatus( p ).IsUpToDate )
            .ToList();

        if ( outOfDatePlugins.Count == 0 )
        {
            AnsiConsole.WriteLine( "All plugins are up to date, nothing to do." );
        }

        var uncleanPlugins = outOfDatePlugins
            .Where( p => !environment.IsPluginClean( p ) )
            .ToList();

        // If we aren't forcing the update then check if everything is clean
        // before we make any changes.
        if ( !ExecuteOptions.Force && uncleanPlugins.Count > 0 )
        {
            foreach ( var plugin in uncleanPlugins )
            {
                AnsiConsole.MarkupLineInterpolated( $"[red]Plugin {plugin.Path} is not clean.[/]" );
            }

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine( "To update anyway, run the command with '--force' option." );

            return Task.FromResult( 1 );
        }

        var progress = AnsiConsole.Progress();

        progress.Start( ctx =>
        {
            foreach ( var plugin in outOfDatePlugins )
            {
                environment.InstallOrUpdatePlugin( plugin, ctx );
            }
        } );

        return Task.FromResult( 0 );
    }
}
