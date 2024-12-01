using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the Rock binary distribution to match what is
/// configured in the environment.
/// </summary>
class RockCommand : Abstractions.BaseModifyCommand
{
    #region Fields

    /// <summary>
    /// The provider of services for this instance.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The object that will be used to access the file system.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The base URL when downloading environment files.
    /// </summary>
    private readonly Option<string?> _sourceOption;

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    #endregion

    #region Properties

    /// <summary>
    /// The base URL to use when downloading environment files.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    #endregion

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
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        Source = context.ParseResult.GetValueForOption( _sourceOption );
        EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
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

        environment.IsDryRun = DryRun;

        var success = await environment.UpdateRockAsync( Source );

        return success ? 0 : 1;
    }
}
