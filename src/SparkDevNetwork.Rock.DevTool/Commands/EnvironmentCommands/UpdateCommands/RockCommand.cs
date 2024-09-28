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

        environment.IsDryRun = ExecuteOptions.DryRun;

        var success = await environment.UpdateRockAsync( ExecuteOptions.Source );

        return success ? 0 : 1;
    }
}
