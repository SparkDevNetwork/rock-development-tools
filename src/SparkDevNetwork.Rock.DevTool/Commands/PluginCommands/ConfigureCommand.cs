using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

/// <summary>
/// Configures a plugin in the environment.
/// </summary>
class ConfigureCommand : Abstractions.BaseModifyCommand<ConfigureCommandOptions>
{
    private readonly IFileSystem _fs;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    /// <summary>
    /// The option that defines the URL for the plugin.
    /// </summary>
    private readonly Option<string?> _urlOption;

    /// <summary>
    /// The option that defines the branch name for the plugin.
    /// </summary>
    private readonly Option<string?> _branchOption;

    /// <summary>
    /// The argument that defines which plugin to operate on.
    /// </summary>
    private readonly Argument<string> _pluginPathArgument;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public ConfigureCommand( IServiceProvider serviceProvider )
        : base( "configure", "Changes the configured parameters of a plugin.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();
        _serviceProvider = serviceProvider;

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        _urlOption = new Option<string?>( "--url", "The URL to set on the plugin." );
        _branchOption = new Option<string?>( "--branch", "The branch name to set on the plugin." );

        _pluginPathArgument = new Argument<string>( "plugin", "The plugin path relative to the environment." );

        AddOption( _environmentOption );
        AddOption( _urlOption );
        AddOption( _branchOption );

        AddArgument( _pluginPathArgument );
    }

    /// <inheritdoc/>
    protected override ConfigureCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
        options.Url = context.ParseResult.GetValueForOption( _urlOption );
        options.Branch = context.ParseResult.GetValueForOption( _branchOption );
        options.PluginPath = context.ParseResult.GetValueForArgument( _pluginPathArgument );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var env = OpenEnvironment();

        if ( env == null )
        {
            return Task.FromResult( 1 );
        }

        var plugin = env.GetPluginData( ExecuteOptions.PluginPath );

        if ( plugin == null )
        {
            Console.MarkupLineInterpolated( $"Plugin [cyan]{ExecuteOptions.PluginPath}[/] was not found in the environment." );
            return Task.FromResult( 1 );
        }

        if ( ExecuteOptions.Url != null )
        {
            plugin.Url = ExecuteOptions.Url;
        }

        if ( ExecuteOptions.Branch != null )
        {
            plugin.Branch = ExecuteOptions.Branch;
        }

        env.Save();

        return Task.FromResult( 0 );
    }

    /// <summary>
    /// Open the environment or return null if the environment is not valid.
    /// </summary>
    /// <returns>An instance of <see cref="DevEnvironment.Environment"/> or <c>null</c>.</returns>
    private DevEnvironment.Environment? OpenEnvironment()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        environmentDirectory = _fs.Path.GetFullPath( environmentDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );
            environment.IsDryRun = ExecuteOptions.DryRun;

            return environment;
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return null;
        }
    }
}
