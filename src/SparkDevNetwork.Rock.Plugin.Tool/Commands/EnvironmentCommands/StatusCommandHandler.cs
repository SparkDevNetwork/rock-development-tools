using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Command handler for <see cref="StatusCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class StatusCommandHandler : Abstractions.BaseActionCommandHandler<StatusCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public StatusCommandHandler( StatusCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override Task<int> InvokeAsync()
    {
        var targetDirectory = Options.Target ?? System.Environment.CurrentDirectory;

        targetDirectory = Path.GetFullPath( targetDirectory );

        var environment = Environment.Open( targetDirectory, AnsiConsole.Console, Options.LoggerFactory );

        if ( environment is null )
        {
            return Task.FromResult( 1 );
        }

        var statuses = environment.GetEnvironmentStatus();

        foreach ( var status in statuses )
        {
            if ( status.IsUpToDate )
            {
                AnsiConsole.Console.WriteLine( $"{status.Name} {status.Message}" );
            }
            else
            {
                AnsiConsole.Console.MarkupLineInterpolated( $"[red]{status.Name} {status.Message}[/]" );

                if ( status is RockStatusItem rockStatus )
                {
                    var badFiles = rockStatus.Files
                        .Where( f => !f.IsUpToDate )
                        .ToList();

                    for ( int i = 0; i < badFiles.Count && i < 10; i++ )
                    {
                        AnsiConsole.Console.MarkupLineInterpolated( $"  [red]{badFiles[i].Name} {badFiles[i].Message}[/]" );
                    }

                    if ( badFiles.Count > 10 )
                    {
                        AnsiConsole.Console.MarkupLineInterpolated( $"  (and {badFiles.Count - 10:N0} more files)" );
                    }
                }
            }
        }

        return Task.FromResult( 0 );
    }
}
