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
class ConfigureCommand : Abstractions.BaseModifyCommand
{
    #region Fields

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The provider of services for this instance.
    /// </summary>
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

    #endregion

    #region Properties

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    /// <summary>
    /// The URL to configure the plugin to use or <c>null</c> to leave unchanged.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// The branch name to configure the plugin to use or <c>null</c> to leave
    /// unchanged.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// The relative path to the plugin.
    /// </summary>
    public string PluginPath { get; set; } = null!;

    #endregion

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
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
        Url = context.ParseResult.GetValueForOption( _urlOption );
        Branch = context.ParseResult.GetValueForOption( _branchOption );
        PluginPath = context.ParseResult.GetValueForArgument( _pluginPathArgument );
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var env = OpenEnvironment();

        if ( env == null )
        {
            return Task.FromResult( 1 );
        }

        var plugin = env.GetPluginData( PluginPath );

        if ( plugin == null )
        {
            Console.MarkupLineInterpolated( $"Plugin [cyan]{PluginPath}[/] was not found in the environment." );
            return Task.FromResult( 1 );
        }

        if ( Url != null )
        {
            plugin.Url = Url;
        }

        if ( Branch != null )
        {
            plugin.Branch = Branch;
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
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        environmentDirectory = _fs.Path.GetFullPath( environmentDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );
            environment.IsDryRun = DryRun;

            return environment;
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return null;
        }
    }
}
