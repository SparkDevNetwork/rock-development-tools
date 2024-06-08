using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.Plugin.Tool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the Rock binary distribution to match what is
/// configured in the environment.
/// </summary>
class RockCommand : Abstractions.BaseModifyCommand<RockCommandOptions>
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IFileSystem _fs;

    /// <summary>
    /// The base URL when downloading environment files.
    /// </summary>
    private readonly Option<string?> _sourceOption;

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    /// <summary>
    /// Creates a command that will handle updating the Rock installation.
    /// </summary>
    public RockCommand( IServiceProvider serviceProvider )
        : base( "rock", "Updates the Rock installation to match the configuration file.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _sourceOption = new Option<string?>( "--source", "The base URL to use when downloading environment files." );

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        AddOption( _sourceOption );
        AddOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override RockCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Source = context.ParseResult.GetValueForOption( _sourceOption );
        options.EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return 1;
        }

        var rock = environment.GetRockInstallation();
        rock.IsDryRun = ExecuteOptions.DryRun;

        if ( !string.IsNullOrEmpty( ExecuteOptions.Source ) )
        {
            rock.RockEnvironmentSourceUrl = ExecuteOptions.Source;
        }

        var rockStatus = rock.GetRockStatus();

        if ( rockStatus.IsUpToDate )
        {
            if ( ExecuteOptions.Force )
            {
                Console.WriteLine( "Rock is up to date, but '--force' option was specified so re-installing." );
            }
            else
            {
                Console.WriteLine( "Rock is up to date." );
                return 0;
            }
        }

        // If we aren't forcing the update then check if everything is clean
        // before we make any changes.
        if ( !ExecuteOptions.Force && !rock.IsRockClean() )
        {
            Console.MarkupLine( "[red]Rock installation is not clean.[/]" );
            Console.WriteLine();
            Console.WriteLine( "To update Rock anyway, run the command with '--force' option." );
            Console.WriteLine();

            return 1;
        }

        Console.WriteLine( "Removing Rock..." );

        if ( ExecuteOptions.Force )
        {
            rock.ForceRemoveRock();
        }
        else
        {
            rock.RemoveRock();
        }

        await rock.InstallRockAsync();

        return 0;
    }
}
