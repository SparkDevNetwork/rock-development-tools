using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

/// <summary>
/// Upgrade command for development environments.
/// </summary>
class UpgradeCommand : Abstractions.BaseActionCommand
{
    #region Fields

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    /// <summary>
    /// The provider of services for this instance.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

    #endregion

    #region Properties

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    #endregion

    /// <summary>
    /// Creates a command that will handle upgrading an existing environment.
    /// </summary>
    public UpgradeCommand( IServiceProvider serviceProvider )
        : base( "upgrade", "Upgrades the environment to the latest configuration.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = _serviceProvider.GetRequiredService<IFileSystem>();

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        AddOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var targetDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        targetDirectory = _fs.Path.GetFullPath( targetDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( targetDirectory, _serviceProvider, warnIfUpgradeNeeded: false );
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return Task.FromResult( 1 );
        }

        if ( !environment.IsUpgradeNeeded() )
        {
            Console.MarkupLine( "[green]The environment is already up to date.[/]" );
            return Task.FromResult( 0 );
        }

        var upgrader = new EnvironmentUpgrader( _fs, Console );

        if ( !upgrader.Upgrade( environment ) )
        {
            return Task.FromResult( 1 );
        }

        Console.MarkupLine( "[green]The environment has been successfully upgraded.[/]" );

        return Task.FromResult( 0 );
    }
}
