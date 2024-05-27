using System.CommandLine;
using System.CommandLine.Invocation;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class StatusCommand : Abstractions.BaseActionCommand<StatusCommandOptions>
{
    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a command that will handle checking on the status of an
    /// existing environment.
    /// </summary>
    public StatusCommand( IServiceProvider serviceProvider )
        : base( "status", "Checks the status of the environment against the configured values.", serviceProvider )
    {
        _serviceProvider = serviceProvider;

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

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var targetDirectory = ExecuteOptions.Target ?? System.Environment.CurrentDirectory;

        targetDirectory = Path.GetFullPath( targetDirectory );

        var environment = Environment.Open( targetDirectory, _serviceProvider );

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
