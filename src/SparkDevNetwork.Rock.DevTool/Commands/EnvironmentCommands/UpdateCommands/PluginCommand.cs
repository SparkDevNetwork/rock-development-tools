using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands.UpdateCommands;

/// <summary>
/// The command to update the plugins to match what is configured in the
/// environment.
/// </summary>
class PluginCommand : Abstractions.BaseModifyCommand<PluginCommandOptions>
{
    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    private readonly IServiceProvider _serviceProvider;

    private readonly IFileSystem _fs;

    /// <summary>
    /// Creates a command that will handle updating the Rock installation.
    /// </summary>
    public PluginCommand( IServiceProvider serviceProvider )
        : base( "plugin", "Updates the plugins to match the configuration file.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        AddOption( _environmentOption );
    }

    /// <inheritdoc/>
    protected override PluginCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
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
            return Task.FromResult( 1 );
        }

        environment.IsDryRun = ExecuteOptions.DryRun;
        environment.IsForce = ExecuteOptions.Force;

        var success = environment.UpdatePlugins();

        return Task.FromResult( success ? 0 : 1 );
    }
}
