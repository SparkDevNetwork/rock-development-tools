using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// Command handler for <see cref="RockCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class RockCommandHandler : Abstractions.BaseModifyCommandHandler<RockCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public RockCommandHandler( RockCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override async Task<int> InvokeAsync()
    {
        var environmentDirectory = Options.Target ?? Directory.GetCurrentDirectory();

        var environment = Environment.Open( environmentDirectory, AnsiConsole.Console, Options.LoggerFactory );

        if ( environment is null )
        {
            return 1;
        }

        environment.IsDryRun = Options.DryRun;

        if ( !string.IsNullOrEmpty( Options.Source ) )
        {
            environment.RockEnvironmentSourceUrl = Options.Source;
        }

        var rockStatus = environment.GetRockStatus();

        if ( rockStatus.IsUpToDate )
        {
            if ( Options.Force )
            {
                AnsiConsole.WriteLine( "Rock is up to date, but '--force' option was specified so re-installing." );
            }
            else
            {
                AnsiConsole.WriteLine( "Rock is up to date." );
                return 0;
            }
        }

        // If we aren't forcing the update then check if everything is clean
        // before we make any changes.
        if ( !Options.Force && !environment.IsRockClean() )
        {
            AnsiConsole.MarkupLine( "[red]Rock installation is not clean.[/]" );
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine( "To update Rock anyway, run the command with '--force' option." );
            AnsiConsole.WriteLine();

            return 1;
        }

        AnsiConsole.WriteLine( "Removing Rock..." );

        if ( Options.Force )
        {
            environment.ForceRemoveRock();
        }
        else
        {
            environment.RemoveRock();
        }

        await environment.InstallRockAsync();

        return 0;
    }
}
