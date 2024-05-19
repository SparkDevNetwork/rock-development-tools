using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// Handles updating and installing environments.
/// </summary>
class Environment
{
    #region Fields

    /// <summary>
    /// The files and directories that should be preserved when installing
    /// new Rock versions.
    /// </summary>
    private static readonly string[] PreservedRockFiles = [
        "RockWeb/web.ConnectionStrings.config",
        "RockWeb/Plugins",
        "RockWeb/App_Data",
    ];

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
    /// The logger to use when writing diagnostic messages.
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Properties

    /// <summary>
    /// The source for the Rock installation archives used when downloading
    /// binary installations.
    /// </summary>
    public string RockEnvironmentSourceUrl { get; set; } = "https://rockrms.blob.core.windows.net/developer/environments";

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
    public Environment( string environmentDirectory, EnvironmentData data, IAnsiConsole console, ILogger logger )
    {
        _environmentDirectory = environmentDirectory;
        _data = data;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// Opens an existing environment from a directory.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="console">The console object to use when writing console messages.</param>
    /// <param name="loggerFactory">The factory to create new logging facilities.</param>
    /// <returns>An instance of <see cref="Environment"/> or <c>null</c> if it could not be opened.</returns>
    public static Environment? Open( string environmentDirectory, IAnsiConsole console, ILoggerFactory loggerFactory )
    {
        var logger = loggerFactory.CreateLogger( typeof( Environment ).Name );
        var environmentFile = Path.Combine( environmentDirectory, EnvironmentData.Filename );

        if ( !File.Exists( environmentFile ) )
        {
            console.MarkupLineInterpolated( $"No environment file was found at [cyan]{environmentFile}[/]." );
            return null;
        }

        var json = File.ReadAllText( environmentFile );
        var environment = JsonSerializer.Deserialize<EnvironmentData>( json );

        if ( environment == null )
        {
            console.MarkupLineInterpolated( $"Invalid environment configuration found in [cyan]{environmentFile}[/]." );
            return null;
        }

        // Validate all plugins and abort if any plugin is not valid.
        if ( environment.Plugins != null && environment.Plugins.Any( p => p.Path == null ) )
        {
            console.WriteLine( "One or more plugins were defined without a path, all plugins must define a path." );
            return null;
        }

        return new Environment( environmentDirectory, environment, console, logger );
    }

    /// <summary>
    /// Installs the configured Rock binary version into the default Rock
    /// directory.
    /// </summary>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public Task InstallRockAsync()
    {
        if ( !SemVersion.TryParse( _data.Rock?.Version, SemVersionStyles.Strict, out var version ) )
        {
            throw new Exception( "Invalid Rock version specified in configuration." );
        }

        return InstallRockVersionAsync( version );
    }

    /// <summary>
    /// Installs the specified Rock binary version into the destination
    /// directory.
    /// </summary>
    /// <param name="rockVersion">The version of Rock to install.</param>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public async Task InstallRockVersionAsync( SemVersion rockVersion )
    {
        var url = $"{RockEnvironmentSourceUrl.TrimEnd( '/' )}/Rock-{rockVersion}.zip";
        var destinationDirectory = Path.Combine( _environmentDirectory, "Rock" );

        _console.MarkupLineInterpolated( $"Installing Rock from [cyan]{url}[/]" );

        var progress = _console.Progress();

        await progress.StartAsync( async ctx =>
        {
            var downloadProgress = ctx.AddTask( "Downloading Rock", true, 1 );
            var extractProgress = ctx.AddTask( IsDryRun ? "Validating archive" : "Extracting files", false, 1 );

            using var client = new HttpClient();
            using var memoryStream = new MemoryStream();

            await client.DownloadAsync( url, memoryStream, p => downloadProgress.Value = p );

            downloadProgress.StopTask();
            ctx.Refresh();

            // Start the extract task and then open the archive and extract.
            extractProgress.StartTask();
            memoryStream.Position = 0;

            using var archive = new ZipArchive( memoryStream );
            ExtractAllFiles( destinationDirectory, archive, extractProgress );

            extractProgress.StopTask();
            ctx.Refresh();
        } );

        _console.MarkupLineInterpolated( $"Installed Rock [cyan]{rockVersion}[/] into [cyan]{destinationDirectory.Replace( '/', Path.DirectorySeparatorChar )}[/]" );
        _console.WriteLine();
    }

    /// <summary>
    /// Extract all the files in the archive into the destination path on disk.
    /// </summary>
    /// <param name="destinationDirectory">The directory to extract the archive into.</param>
    /// <param name="archive">The archive to be extracted.</param>
    /// <param name="progress">If not <c>null</c> then this task will be updated with progress.</param>
    private void ExtractAllFiles( string destinationDirectory, ZipArchive archive, ProgressTask? progress = null )
    {
        for ( int i = 0; i < archive.Entries.Count; i++ )
        {
            var entry = archive.Entries[i];

            if ( entry.FullName.EndsWith( '/' ) || entry.FullName.EndsWith( '\\' ) )
            {
                continue;
            }

            var destinationFilePath = Path.Combine( destinationDirectory, entry.FullName );
            var fileDirectory = Path.GetDirectoryName( destinationFilePath );

            if ( !IsDryRun )
            {
                if ( fileDirectory != null && !Directory.Exists( fileDirectory ) )
                {
                    Directory.CreateDirectory( fileDirectory );
                }

                entry.ExtractToFile( Path.Combine( destinationDirectory, entry.FullName ), true );
            }

            if ( progress != null )
            {
                progress.Value = ( float ) i / archive.Entries.Count;
            }
        }

        if ( progress != null )
        {
            progress.Value = 1;
        }
    }

    /// <summary>
    /// Removes the current installation of Rock from the file system using
    /// the standard Rock directory.
    /// </summary>
    public void RemoveRock()
    {
        var destinationDirectory = Path.Combine( _environmentDirectory, "Rock" );

        RemoveRock( destinationDirectory );
    }

    /// <summary>
    /// Removes the current installation of Rock from the file system.
    /// </summary>
    /// <param name="targetDirectory">The directory that Rock is installed in.</param>
    public void RemoveRock( string targetDirectory )
    {
        if ( !Directory.Exists( targetDirectory ) )
        {
            return;
        }

        if ( IsDryRun )
        {
            var relativeTargetDirectory = Path.GetRelativePath( Directory.GetCurrentDirectory(), targetDirectory );

            _console.WriteLine( $"Remove {relativeTargetDirectory}" );
        }

        bool RemoveDirectory( string directory )
        {
            bool removeDirectory = true;

            foreach ( var dirpath in Directory.EnumerateDirectories( directory ) )
            {
                var relativepath = Path.GetRelativePath( targetDirectory, dirpath );

                if ( PreservedRockFiles.Contains( relativepath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    if ( IsDryRun )
                    {
                        _console.WriteLine( $"  Preserve {Path.GetRelativePath( Directory.GetCurrentDirectory(), dirpath )}" );
                    }

                    removeDirectory = false;
                }
                else if ( !RemoveDirectory( dirpath ) )
                {
                    removeDirectory = false;
                }
            }

            foreach ( var filepath in Directory.EnumerateFiles( directory ) )
            {
                var relativepath = Path.GetRelativePath( targetDirectory, filepath );

                if ( PreservedRockFiles.Contains( relativepath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    if ( IsDryRun )
                    {
                        _console.WriteLine( $"  Preserve {Path.GetRelativePath( Directory.GetCurrentDirectory(), filepath )}" );
                    }

                    removeDirectory = false;
                }
                else if ( !IsDryRun )
                {
                    File.Delete( filepath );
                }
            }

            if ( removeDirectory && !IsDryRun )
            {
                Directory.Delete( directory, false );
            }

            return removeDirectory;
        }

        RemoveDirectory( targetDirectory );
    }

    /// <summary>
    /// Gets the status of the environment by way of individual status
    /// items to describe the status of Rock and each plugin.
    /// </summary>
    /// <returns>A list of status items.</returns>
    public List<EnvironmentStatusItem> GetEnvironmentStatus()
    {
        var statuses = new List<EnvironmentStatusItem>
        {
            GetRockStatus()
        };

        if ( _data.Plugins != null )
        {
            foreach ( var plugin in _data.Plugins )
            {
                statuses.Add( GetPluginStatus( plugin ) );
            }
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

    /// <summary>
    /// Checks if the Rock installation is up to date. This is a best guess
    /// based on the version number of the Rock.dll file.
    /// </summary>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public EnvironmentStatusItem GetRockStatus()
    {
        if ( _data.Rock == null || _data.Rock.Version == "custom" )
        {
            return new EnvironmentStatusItem( "Rock", null );
        }

        if ( !SemVersion.TryParse( _data.Rock.Version, SemVersionStyles.Strict, out var version ) )
        {
            _logger.LogError( "Unable to parse Rock version number '{version}'.", _data.Rock.Version );
            return new EnvironmentStatusItem( "Rock", "has an invalid version number.", null );
        }

        var rockDllPath = Path.Combine( _environmentDirectory, "Rock", "RockWeb", "Bin", "Rock.dll" );
        if ( !File.Exists( rockDllPath ) )
        {
            _logger.LogInformation( "No Rock assembly was found at {filename}.", rockDllPath );
            return new EnvironmentStatusItem( "Rock", "is not installed.", null );
        }

        var asmName = AssemblyName.GetAssemblyName( rockDllPath );

        if ( asmName.Version == null )
        {
            _logger.LogError( "No version number found in Rock assembly." );
            return new EnvironmentStatusItem( "Rock", "is not installed.", null );
        }

        if ( version.Major < 2 )
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor
                && version.Patch == asmName.Version.Build;

            if ( !doesVersionMatch )
            {
                _logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", asmName.Version, version );
                return new EnvironmentStatusItem( "Rock", $"version installed is {version} but should be {asmName.Version}.", null );
            }
        }
        else
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor;

            if ( !doesVersionMatch )
            {
                _logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", version, asmName.Version );
                return new EnvironmentStatusItem( "Rock", $"version installed is {version} but should be {asmName.Version}.", null );
            }
        }

        return new EnvironmentStatusItem( "Rock", null );
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public EnvironmentStatusItem GetPluginStatus( PluginData plugin )
    {
        if ( plugin.Url == null )
        {
            _logger.LogError( "Plugin {path} is missing url.", plugin.Path );
            return new EnvironmentStatusItem( "Unknown", "plugin is missing path.", plugin );
        }

        var pluginDirectory = Path.Combine( _environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return new EnvironmentStatusItem( plugin.Path, "is not a git repository.", plugin );
        }

        if ( plugin.Branch == null )
        {
            return new EnvironmentStatusItem( plugin.Path, plugin );
        }

        var repository = new Repository( pluginDirectory );
        var reference = repository.Head.Reference.TargetIdentifier;

        if ( !reference.StartsWith( "refs/heads/" ) )
        {
            _logger.LogInformation( "Plugin {path} is not on a branch.", plugin.Path );
            return new EnvironmentStatusItem( plugin.Path, "is not on a branch.", plugin );
        }

        var branch = reference.Substring( 11 );

        if ( plugin.Branch != branch )
        {
            _logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", plugin.Path, branch, plugin.Branch );
            return new EnvironmentStatusItem( plugin.Path, $"is on branch {branch} but should be {plugin.Branch}.", plugin );
        }

        return new EnvironmentStatusItem( plugin.Path, plugin );
    }

    /// <summary>
    /// Checks if the Rock installation is clean. A clean installation means that
    /// nothing has been modified from the original files that were put in place.
    /// </summary>
    /// <returns><c>true</c> if the Rock installation is in a clean state; otherwise <c>false</c>.</returns>
    public bool IsRockClean()
    {
        // TODO: I think we need to create a .environment-lock.json file or
        // something like that which contains every file installed for the Rock
        // installation as well as the SHA1 of the contents. Then in this
        // method we use that to validate to make sure none of those files have
        // changed. This should also make it easier to know which files are
        // safe to delete.
        return false;
    }

    /// <summary>
    /// Checks if the plugin is clean. A clean installation means that the git
    /// repository is in a clean state.
    /// </summary>
    /// <returns><c>true</c> if the plugin is in a clean state; otherwise <c>false</c>.</returns>
    public bool IsPluginClean( PluginData plugin )
    {
        var pluginDirectory = Path.Combine( _environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        // If the directory does not exist, it is considered clean so that
        // an update command can execute.
        if ( !Directory.Exists( pluginDirectory ) )
        {
            return true;
        }

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return false;
        }

        using var repository = new Repository( pluginDirectory );

        return !repository.RetrieveStatus().IsDirty;
    }
}
