using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.DevTool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

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
            var friendlyPath = fs.Path.GetFriendlyPath( environmentFile );

            throw new InvalidEnvironmentException( $"No environment file was found at {friendlyPath}." );
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
        }

        return new Environment( environmentDirectory, data, serviceProvider );
    }

    /// <summary>
    /// Saves the environment data back to the environment definition file.
    /// </summary>
    public void Save()
    {
        var environmentFile = _fs.Path.Combine( _environmentDirectory, EnvironmentData.Filename );
        var json = JsonSerializer.Serialize( _data, Support.SerializerOptions );

        if ( IsDryRun )
        {
            var friendlyPath = _fs.Path.GetFriendlyPath( environmentFile );

            if ( _fs.File.Exists( environmentFile ) )
            {
                _console.MarkupLineInterpolated( $"Replace [cyan]{friendlyPath}[/]." );
            }
            else
            {
                _console.MarkupLineInterpolated( $"Create [cyan]{friendlyPath}[/]." );
            }
        }
        else
        {
            _fs.File.WriteAllText( environmentFile, json );
        }
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
            return PluginInstallation.Open( path, p, _fs, _logger );
        } )
        .ToList();
    }

    /// <summary>
    /// Gets the data that defines the plugin at the specified path. This can
    /// be used to modify the plugin definition in the environment.
    /// </summary>
    /// <param name="path">The relative path to the plugin.</param>
    /// <returns>A <see cref="PluginReferenceData"/> instance or <c>null</c>.</returns>
    public PluginReferenceData? GetPluginData( string path )
    {
        return _data.Plugins.FirstOrDefault( p => p.Path == path );
    }

    /// <summary>
    /// Adds a new plugin to the environment. No vlaidation is performed to see
    /// if the plugin is actually valid.
    /// </summary>
    /// <param name="relativePath">The path, relative to the environment.</param>
    public void AddPlugin( string relativePath )
    {
        _data.Plugins.Add( new PluginReferenceData
        {
            Path = relativePath.Replace( '\\', '/' )
        } );
    }

    /// <summary>
    /// Setup the plugin for use in this environment. This performs any
    /// modifications to the file system that are required for the plugin to
    /// be functional in this environment.
    /// </summary>
    /// <param name="plugin">The plugin to setup.</param>
    /// <returns><c>true</c> if the plugin was setup; otherwise <c>false</c>.</returns>
    public bool SetupPlugin( PluginInstallation plugin )
    {
        var webFormsDirectory = _fs.Path.Combine( _environmentDirectory, plugin.Path, "WebForms" );
        var junctionDirectory = _fs.Path.Combine( _environmentDirectory, "Rock", "RockWeb", "Plugins", plugin.OrganizationPluginPath, plugin.Code );

        if ( _fs.Directory.Exists( webFormsDirectory ) )
        {
            if ( !CreateJunction( junctionDirectory, webFormsDirectory ) )
            {
                return false;
            }
        }

        return true;
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
            statuses.Add( GetPluginStatus( plugin ) );
        }

        return statuses;
    }

    /// <summary>
    /// Gets the status of a plugin in the environment. This calls the plugin
    /// <see cref="PluginInstallation.GetStatus"/> method and then performs
    /// additional checks related to the plugin in the environment.
    /// </summary>
    /// <param name="plugin">The plugin to be checked.</param>
    /// <returns>A status item that represents the status of the plugin.</returns>
    public StatusItem GetPluginStatus( PluginInstallation plugin )
    {
        var status = plugin.GetStatus();

        if ( !status.IsUpToDate )
        {
            return status;
        }

        var webFormsDirectory = _fs.Path.Combine( _environmentDirectory, plugin.Path, "WebForms" );
        var junctionDirectory = _fs.Path.Combine( _environmentDirectory, "Rock", "RockWeb", "Plugins", plugin.OrganizationPluginPath, plugin.Code );

        if ( _fs.Directory.Exists( webFormsDirectory ) )
        {
            var d = _fs.DirectoryInfo.New( junctionDirectory );

            if ( !d.Exists )
            {
                return new PluginStatusItem( plugin.Name, "is missing WebForms junction directory.", plugin.Data );
            }
            else
            {
                if ( d.LinkTarget != webFormsDirectory )
                {
                    return new PluginStatusItem( plugin.Name, "has incorrect WebForms junction directory.", plugin.Data );
                }
            }
        }

        return status;
    }

    /// <summary>
    /// Checks if the environment is up to date with the configuration.
    /// </summary>
    /// <returns><c>true</c> if the environment is already up to date; otherwise <c>false</c>.</returns>
    public bool IsEnvironmentUpToDate()
    {
        return GetEnvironmentStatus().All( s => s.IsUpToDate );
    }

    /// <summary>
    /// Gets the configured Rock version for the environment.
    /// </summary>
    /// <returns>An instance of <see cref="SemVersion"/> or <c>null</c>.</returns>
    public SemVersion? GetRockVersion()
    {
        if ( _data.Rock?.Version == null )
        {
            return null;
        }

        if ( !SemVersion.TryParse( _data.Rock.Version, SemVersionStyles.Strict, out var version ) )
        {
            return null;
        }

        return version;
    }

    /// <summary>
    /// Gets the name of the organization defined in the environment.
    /// </summary>
    /// <returns>A string or <c>null</c>.</returns>
    public string? GetOrganizationName()
    {
        return _data.Organization?.Name;
    }

    /// <summary>
    /// Gets the name of the organization code defined in the environment.
    /// </summary>
    /// <returns>A string or <c>null</c>.</returns>
    public string? GetOrganizationCode()
    {
        return _data.Organization?.Code;
    }

    /// <summary>
    /// Creates a Windows Junction at <paramref name="junctionDirectory"/>. These
    /// are kind of like symbolic links for directories, except they don't require
    /// administrative access to create.
    /// </summary>
    /// <param name="junctionDirectory">The directory path to create the junction at.</param>
    /// <param name="targetDirectory">The target of the junction.</param>
    /// <returns></returns>
    private bool CreateJunction( string junctionDirectory, string targetDirectory )
    {
        var junctionParentDirectory = _fs.Path.GetDirectoryName( junctionDirectory );

        if ( !OperatingSystem.IsWindows() )
        {
            _logger.LogError( "Junctions are only supported on Windows." );
            return false;
        }

        var d = _fs.DirectoryInfo.New( junctionDirectory );

        if ( d.Exists )
        {
            if ( d.LinkTarget == targetDirectory )
            {
                // Junction is already setup correctly.
                return true;
            }
            else if ( IsDryRun )
            {
                _console.MarkupLineInterpolated( $"Remove directory [cyan]{_fs.Path.GetFriendlyPath( junctionDirectory )}[/]." );
            }
            else
            {
                _fs.Directory.Delete( junctionDirectory, true );
            }
        }

        if ( junctionParentDirectory != null )
        {
            if ( IsDryRun )
            {
                _console.MarkupLineInterpolated( $"Create directory [cyan]{_fs.Path.GetFriendlyPath( junctionParentDirectory )}[/c]." );
            }
            else
            {
                _fs.Directory.CreateDirectory( junctionParentDirectory );
            }
        }

        if ( IsDryRun )
        {
            _console.MarkupLineInterpolated( $"Create junction [cyan]{_fs.Path.GetFriendlyPath( junctionDirectory )}[/c]." );
            return true;
        }

        var info = new ProcessStartInfo
        {
            FileName = "cmd",
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        info.ArgumentList.Add( "/c" );
        info.ArgumentList.Add( "mklink" );
        info.ArgumentList.Add( "/J" );
        info.ArgumentList.Add( junctionDirectory );
        info.ArgumentList.Add( targetDirectory );

        var process = Process.Start( info );

        process?.WaitForExit();

        if ( process == null || process.ExitCode != 0 )
        {
            _logger.LogError( "Unable to create junction at {path}.", junctionDirectory );
            return false;
        }

        return true;
    }
}
