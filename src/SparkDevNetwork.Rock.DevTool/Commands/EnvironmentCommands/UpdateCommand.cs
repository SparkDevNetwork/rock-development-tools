using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

/// <summary>
/// The command to update the plugins and the Rock binary distribution to match
/// what is configured in the environment.
/// </summary>
class UpdateCommand : Abstractions.BaseModifyCommand<UpdateCommandOptions>
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
    /// Creates a command that will handle updating the environment.
    /// </summary>
    public UpdateCommand( IServiceProvider serviceProvider )
        : base( "update", "Updates the environment.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _sourceOption = new Option<string?>( "--source", "The base URL to use when downloading environment files." );

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        AddOption( _sourceOption );
        AddOption( _environmentOption );

        AddCommand( ActivatorUtilities.CreateInstance<UpdateCommands.RockCommand>( serviceProvider ) );
        AddCommand( ActivatorUtilities.CreateInstance<UpdateCommands.PluginCommand>( serviceProvider ) );
    }

    /// <inheritdoc/>
    protected override UpdateCommandOptions GetOptions( InvocationContext context )
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
        environment.IsForce = ExecuteOptions.Force;

        var success = await environment.UpdateRockAsync( ExecuteOptions.Source );

        if ( !success )
        {
            return 1;
        }

        success = environment.UpdatePlugins();

        return success ? 0 : 1;
    }
}
