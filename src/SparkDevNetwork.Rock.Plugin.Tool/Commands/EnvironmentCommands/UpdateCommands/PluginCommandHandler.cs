using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// Command handler for <see cref="PluginCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class PluginCommandHandler : Abstractions.BaseModifyCommandHandler<PluginCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public PluginCommandHandler( PluginCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override Task<int> InvokeAsync()
    {
        var environmentDirectory = Options.Target ?? Directory.GetCurrentDirectory();

        var environment = Environment.Open( environmentDirectory, AnsiConsole.Console, Options.LoggerFactory );

        if ( environment is null )
        {
            return Task.FromResult( 1 );
        }

        environment.IsDryRun = Options.DryRun;

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
        if ( !Options.Force && uncleanPlugins.Count > 0 )
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
