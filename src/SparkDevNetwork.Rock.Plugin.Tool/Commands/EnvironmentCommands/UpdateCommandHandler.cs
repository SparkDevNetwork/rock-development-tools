using System.Reflection;
using System.Text.Json;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Command handler for <see cref="UpdateCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class UpdateCommandHandler : Abstractions.BaseModifyCommandHandler<UpdateCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public UpdateCommandHandler( UpdateCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override async Task<int> InvokeAsync()
    {
        var environmentDirectory = Options.Target ?? Directory.GetCurrentDirectory();
        var environmentFile = Path.Combine( environmentDirectory, EnvironmentData.Filename );

        if ( !File.Exists( environmentFile ) )
        {
            AnsiConsole.MarkupLineInterpolated( $"No environment file was found at [cyan]{environmentFile}[/]." );
            return 1;
        }

        var json = File.ReadAllText( environmentFile );
        var environment = JsonSerializer.Deserialize<EnvironmentData>( json );

        if ( environment == null )
        {
            AnsiConsole.MarkupLineInterpolated( $"Invalid environment configuration found in [cyan]{environmentFile}[/]." );
            return 1;
        }

        var helper = new EnvironmentHelper( Logger )
        {
            IsDryRun = Options.DryRun
        };

        if ( helper.IsEnvironmentUpToDate( environmentDirectory, environment ) )
        {
            AnsiConsole.WriteLine( "Environment is up to date." );
            return 0;
        }

        return 0;
    }

}
