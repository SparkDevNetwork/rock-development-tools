using System.IO.Abstractions;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.DevEnvironment;

/// <summary>
/// Handles updating and installing environments.
/// </summary>
class Environment
{
    #region Fields

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    private readonly string _environmentDirectory;

    /// <summary>
    /// The data that represents the environment configuration.
    /// </summary>
    private readonly EnvironmentData _data;

    /// <summary>
    /// The console to use when writing messages to standard output.
    /// </summary>
    private readonly IAnsiConsole _console;

    /// <summary>
    /// The object that provides access to the file system.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The logger to use when writing diagnostic messages.
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Properties

    /// <summary>
    /// Determines if this operation is a dry-run. When <c>true</c> no changes
    /// will be made to files on disk.
    /// </summary>
    public bool IsDryRun { get; set; }

    #endregion

    /// <summary>
    /// Creates a new instance of the environment helper.
    /// </summary>
    /// <param name="logger">The logger to use when writing diagnostic messages.</param>
    private Environment( string environmentDirectory, EnvironmentData data, IServiceProvider serviceProvider )
    {
        _environmentDirectory = environmentDirectory;
        _data = data;
        _console = serviceProvider.GetRequiredService<IAnsiConsole>();
        _fs = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetRequiredService<ILogger>();
    }

    /// <summary>
    /// Opens an existing environment from a directory.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="console">The console object to use when writing console messages.</param>
    /// <param name="loggerFactory">The factory to create new logging facilities.</param>
    /// <returns>An instance of <see cref="Environment"/> or <c>null</c> if it could not be opened.</returns>
    public static Environment Open( string environmentDirectory, IServiceProvider serviceProvider )
    {
        var fs = serviceProvider.GetRequiredService<IFileSystem>();
        var environmentFile = fs.Path.Combine( environmentDirectory, EnvironmentData.Filename );

        if ( !fs.File.Exists( environmentFile ) )
        {
            throw new InvalidEnvironmentException( $"No environment file was found at {environmentFile}." );
        }

        var json = fs.File.ReadAllText( environmentFile );
        var data = JsonSerializer.Deserialize<EnvironmentData>( json )
            ?? throw new InvalidEnvironmentException( $"Invalid environment configuration found in {environmentFile}." );

        // Validate all plugins and abort if any plugin is not valid.
        foreach ( var plugin in data.Plugins )
        {
            if ( string.IsNullOrWhiteSpace( plugin.Path ) )
            {
                throw new InvalidEnvironmentException( "One or more plugins were defined without a path, all plugins must define a path." );
            }

            if ( string.IsNullOrWhiteSpace( plugin.Url ) )
            {
                throw new InvalidEnvironmentException( $"No url defined for plugin {plugin.Path}." );
            }

            if ( string.IsNullOrWhiteSpace( plugin.Branch ) )
            {
                throw new InvalidEnvironmentException( $"No branch defined for plugin {plugin.Path}." );
            }
        }

        return new Environment( environmentDirectory, data, serviceProvider );
    }

    /// <summary>
    /// Gets a new object instance that will handle the Rock installation.
    /// </summary>
    /// <returns>A new instance of <see cref="RockInstallation"/>.</returns>
    public RockInstallation GetRockInstallation()
    {
        var rockPath = _fs.Path.Combine( _environmentDirectory, "Rock" );
        var rock = new RockInstallation( rockPath, _data.Rock, _fs, _console, _logger )
        {
            IsDryRun = IsDryRun
        };

        return rock;
    }

    /// <summary>
    /// Gets all the plugins defined in this environment.
    /// </summary>
    /// <returns>A list of plugins.</returns>
    public List<PluginInstallation> GetPlugins()
    {
        return _data.Plugins.Select( p =>
        {
            var path = _fs.Path.Combine( _environmentDirectory, p.Path.Replace( '/', Path.PathSeparator ) );
            return new PluginInstallation( path, p, _fs, _logger );
        } )
        .ToList();
    }

    /// <summary>
    /// Gets the status of the environment by way of individual status
    /// items to describe the status of Rock and each plugin.
    /// </summary>
    /// <returns>A list of status items.</returns>
    public List<StatusItem> GetEnvironmentStatus()
    {
        var statuses = new List<StatusItem>
        {
            GetRockInstallation().GetRockStatus()
        };

        foreach ( var plugin in GetPlugins() )
        {
            statuses.Add( plugin.GetStatus() );
        }

        return statuses;
    }

    /// <summary>
    /// Checks if the environment is up to date with the configuration.
    /// </summary>
    /// <returns><c>true</c> if the environment is already up to date; otherwise <c>false</c>.</returns>
    public bool IsEnvironmentUpToDate()
    {
        return GetEnvironmentStatus().All( s => s.IsUpToDate );
    }
}
