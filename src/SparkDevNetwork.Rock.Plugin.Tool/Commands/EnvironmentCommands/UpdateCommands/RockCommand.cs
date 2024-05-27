using System.CommandLine;
using System.CommandLine.Invocation;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the Rock binary distribution to match what is
/// configured in the environment.
/// </summary>
class RockCommand : Abstractions.BaseModifyCommand<RockCommandOptions>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The base URL when downloading environment files.
    /// </summary>
    private readonly Option<string?> _sourceOption;

    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    /// <summary>
    /// Creates a command that will handle updating the Rock installation.
    /// </summary>
    public RockCommand( IServiceProvider serviceProvider )
        : base( "rock", "Updates the Rock installation to match the configuration file.", serviceProvider )
    {
        _serviceProvider = serviceProvider;

        _sourceOption = new Option<string?>( "--source", "The base URL to use when downloading environment files." );

        _targetOption = new Option<string?>( "--target", "The directory that contains the environment." );
        _targetOption.AddAlias( "-t" );

        AddOption( _sourceOption );
        AddOption( _targetOption );
    }

    /// <inheritdoc/>
    protected override RockCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Source = context.ParseResult.GetValueForOption( _sourceOption );
        options.Target = context.ParseResult.GetValueForOption( _targetOption );

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var environmentDirectory = ExecuteOptions.Target ?? Directory.GetCurrentDirectory();

        var environment = Environment.Open( environmentDirectory, _serviceProvider );

        if ( environment is null )
        {
            return 1;
        }

        environment.IsDryRun = ExecuteOptions.DryRun;

        if ( !string.IsNullOrEmpty( ExecuteOptions.Source ) )
        {
            environment.RockEnvironmentSourceUrl = ExecuteOptions.Source;
        }

        var rockStatus = environment.GetRockStatus();

        if ( rockStatus.IsUpToDate )
        {
            if ( ExecuteOptions.Force )
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
        if ( !ExecuteOptions.Force && !environment.IsRockClean() )
        {
            AnsiConsole.MarkupLine( "[red]Rock installation is not clean.[/]" );
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine( "To update Rock anyway, run the command with '--force' option." );
            AnsiConsole.WriteLine();

            return 1;
        }

        AnsiConsole.WriteLine( "Removing Rock..." );

        if ( ExecuteOptions.Force )
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
