using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class StatusCommand : Abstractions.BaseActionCommand<StatusCommandOptions>
{
    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    private readonly IServiceProvider _serviceProvider;

    private readonly IFileSystem _fs;

    /// <summary>
    /// Creates a command that will handle checking on the status of an
    /// existing environment.
    /// </summary>
    public StatusCommand( IServiceProvider serviceProvider )
        : base( "status", "Checks the status of the environment against the configured values.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = _serviceProvider.GetRequiredService<IFileSystem>();

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        AddOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override StatusCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var targetDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        targetDirectory = _fs.Path.GetFullPath( targetDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( targetDirectory, _serviceProvider );
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return Task.FromResult( 1 );
        }

        var statuses = environment.GetEnvironmentStatus();

        foreach ( var status in statuses )
        {
            if ( status.IsUpToDate )
            {
                Console.WriteLine( $"{status.Name} {status.Message}" );
            }
            else
            {
                Console.MarkupLineInterpolated( $"[red]{status.Name} {status.Message}[/]" );

                if ( status is RockStatusItem rockStatus )
                {
                    var badFiles = rockStatus.Files
                        .Where( f => !f.IsUpToDate )
                        .ToList();

                    for ( int i = 0; i < badFiles.Count && i < 10; i++ )
                    {
                        Console.MarkupLineInterpolated( $"  [red]{badFiles[i].Name} {badFiles[i].Message}[/]" );
                    }

                    if ( badFiles.Count > 10 )
                    {
                        Console.MarkupLineInterpolated( $"  (and {badFiles.Count - 10:N0} more files)" );
                    }
                }
            }
        }

        return Task.FromResult( 0 );
    }
}
