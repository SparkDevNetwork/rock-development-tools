using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
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
    /// The default serializer options that outputs with indentation.
    /// </summary>
    static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true
    };

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
            var hashes = ExtractAllFiles( destinationDirectory, archive, extractProgress );

            var rockObject = new RockInstallationData
            {
                Files = hashes
            };
            var rockJson = JsonSerializer.Serialize( rockObject, SerializerOptions );
            File.WriteAllText( Path.Combine( destinationDirectory, ".rock.json" ), rockJson );

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
    /// <returns>A dictionary of relative filename paths and the SHA1 hash associated with them that were installed.</returns>
    private Dictionary<string, string> ExtractAllFiles( string destinationDirectory, ZipArchive archive, ProgressTask? progress = null )
    {
        var fileHashes = new Dictionary<string, string>();

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

            using var fileStream = entry.Open();
            var hash = CalculateHexHash( fileStream );

            fileHashes[entry.FullName.Replace( "\\", "/" )] = hash;

            if ( progress != null )
            {
                progress.Value = ( float ) i / archive.Entries.Count;
            }
        }

        if ( progress != null )
        {
            progress.Value = 1;
        }

        return fileHashes;
    }

    /// <summary>
    /// Removes the current installation of Rock from the file system using
    /// the standard Rock directory. This method uses the installation log
    /// file to determine which files to delete.
    /// </summary>
    public void RemoveRock()
    {
        var rockDirectory = Path.Combine( _environmentDirectory, "Rock" );
        var rockStatusFile = Path.Combine( rockDirectory, ".rock.json" );

        if ( !File.Exists( rockStatusFile ) )
        {
            // Assume nothing to do.
            return;
        }

        var rockInstallation = JsonSerializer.Deserialize<RockInstallationData>( File.ReadAllText( rockStatusFile ) );

        if ( rockInstallation == null || rockInstallation.Files == null )
        {
            _logger.LogError( "Rock installation file {file} was not valid.", rockStatusFile );

            throw new InvalidOperationException( "Rock installation is not clean." );
        }

        var directoryPaths = new List<string>();

        foreach ( var file in rockInstallation.Files )
        {
            var filePath = Path.Combine( rockDirectory, file.Key );

            // Remove the file if it exists.
            if ( File.Exists( filePath ) )
            {
                File.Delete( filePath );
            }

            var fileDirectory = Path.GetDirectoryName( filePath );

            if ( fileDirectory != null && !directoryPaths.Contains( fileDirectory ) )
            {
                directoryPaths.Add( fileDirectory );
            }
        }

        File.Delete( rockStatusFile );

        // Delete any directory that is now empty.
        foreach ( var directoryPath in directoryPaths.OrderByDescending( d => d.Length ) )
        {
            // Not every directory has a file, so we also need to check
            // parent directories to see if they are empty in case we are
            // dealing with a grand-parent directory that only had
            // sub-directories but no files.
            var path = directoryPath;

            while ( Path.GetFullPath( path ) != Path.GetFullPath( rockDirectory ) )
            {
                if ( Directory.Exists( path ) )
                {
                    if ( Directory.GetFiles( path ).Length > 0 || Directory.GetDirectories( path ).Length > 0 )
                    {
                        break;
                    }

                    Directory.Delete( path );
                }

                path = Path.GetDirectoryName( path );

                if ( path == null )
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Removes the current installation of Rock from the file system using
    /// the standard Rock directory. This is a forceful removal that just
    /// deletes everything except the specific whitelist files.
    /// </summary>
    public void ForceRemoveRock()
    {
        var targetDirectory = Path.Combine( _environmentDirectory, "Rock" );

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
    public List<StatusItem> GetEnvironmentStatus()
    {
        var statuses = new List<StatusItem>
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
    public RockStatusItem GetRockStatus()
    {
        if ( _data.Rock == null || _data.Rock.Version == "custom" )
        {
            return new RockStatusItem( [] );
        }

        if ( !SemVersion.TryParse( _data.Rock.Version, SemVersionStyles.Strict, out var expectedVersion ) )
        {
            _logger.LogError( "Unable to parse Rock version number '{version}'.", _data.Rock.Version );
            return new RockStatusItem( "has an invalid version number.", null );
        }

        var fileStatuses = GetRockFileStatuses();

        if ( fileStatuses != null && fileStatuses.Any( f => !f.IsUpToDate ) )
        {
            return new RockStatusItem( "has been modified since installation.", fileStatuses );
        }

        var rockDllPath = Path.Combine( _environmentDirectory, "Rock", "RockWeb", "Bin", "Rock.dll" );
        if ( !File.Exists( rockDllPath ) )
        {
            _logger.LogInformation( "No Rock assembly was found at {filename}.", rockDllPath );
            return new RockStatusItem( "is not installed.", fileStatuses );
        }

        if ( fileStatuses == null )
        {
            return new RockStatusItem( "was not installed correctly.", null );
        }

        var asmName = AssemblyName.GetAssemblyName( rockDllPath );

        if ( asmName.Version == null )
        {
            _logger.LogError( "No version number found in Rock assembly." );
            return new RockStatusItem( "is not installed.", fileStatuses );
        }

        if ( expectedVersion.Major < 2 )
        {
            var doesVersionMatch = expectedVersion.Major == asmName.Version.Major
                && expectedVersion.Minor == asmName.Version.Minor
                && expectedVersion.Patch == asmName.Version.Build;

            if ( !doesVersionMatch )
            {
                _logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", asmName.Version, expectedVersion );
                return new RockStatusItem( $"version installed is {asmName.Version} but should be {expectedVersion}.", fileStatuses );
            }
        }
        else
        {
            var doesVersionMatch = expectedVersion.Major == asmName.Version.Major
                && expectedVersion.Minor == asmName.Version.Minor;

            if ( !doesVersionMatch )
            {
                _logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", expectedVersion, asmName.Version );
                return new RockStatusItem( $"version installed is {asmName.Version} but should be {expectedVersion}.", fileStatuses );
            }
        }

        return new RockStatusItem( fileStatuses );
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public PluginStatusItem GetPluginStatus( PluginData plugin )
    {
        if ( plugin.Url == null )
        {
            _logger.LogError( "Plugin {path} is missing url.", plugin.Path );
            return new PluginStatusItem( "Unknown", "plugin is missing path.", plugin );
        }

        var pluginDirectory = Path.Combine( _environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            _logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return new PluginStatusItem( plugin.Path, "is not a git repository.", plugin );
        }

        if ( plugin.Branch == null )
        {
            return new PluginStatusItem( plugin.Path, plugin );
        }

        var repository = new Repository( pluginDirectory );
        var reference = repository.Head.Reference.TargetIdentifier;

        if ( !reference.StartsWith( "refs/heads/" ) )
        {
            _logger.LogInformation( "Plugin {path} is not on a branch.", plugin.Path );
            return new PluginStatusItem( plugin.Path, "is not on a branch.", plugin );
        }

        var branch = reference.Substring( 11 );

        if ( plugin.Branch != branch )
        {
            _logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", plugin.Path, branch, plugin.Branch );
            return new PluginStatusItem( plugin.Path, $"is on branch {branch} but should be {plugin.Branch}.", plugin );
        }

        return new PluginStatusItem( plugin.Path, plugin );
    }

    /// <summary>
    /// Gets a list of status items that reflect the Rock installation status
    /// for each individual file.
    /// </summary>
    /// <returns>A list of <see cref="StatusItem"/> objects.</returns>
    public List<StatusItem>? GetRockFileStatuses()
    {
        var rockDirectory = Path.Combine( _environmentDirectory, "Rock" );
        var rockStatusFile = Path.Combine( rockDirectory, ".rock.json" );

        if ( !File.Exists( rockStatusFile ) )
        {
            _logger.LogInformation( "Rock installation file {file} is missing.", rockStatusFile );
            return null;
        }

        var rockInstallation = JsonSerializer.Deserialize<RockInstallationData>( File.ReadAllText( rockStatusFile ) );

        if ( rockInstallation == null || rockInstallation.Files == null )
        {
            _logger.LogError( "Rock installation file {file} was not valid.", rockStatusFile );
            return null;
        }

        var fileStatuses = new List<StatusItem>();

        foreach ( var file in rockInstallation.Files )
        {
            var filePath = file.Key.Replace( '/', Path.DirectorySeparatorChar );

            filePath = Path.Combine( rockDirectory, filePath );
            var relativePath = Path.GetRelativePath( _environmentDirectory, filePath );

            if ( !File.Exists( filePath ) )
            {
                fileStatuses.Add( new StatusItem( relativePath, "is missing." ) );
                continue;
            }

            var hash = CalculateHexHash( filePath );

            if ( hash != file.Value )
            {
                fileStatuses.Add( new StatusItem( relativePath, "Has been modified." ) );
                continue;
            }
        }

        return fileStatuses;
    }

    /// <summary>
    /// Checks if the Rock installation is clean. A clean installation means that
    /// nothing has been modified from the original files that were put in place.
    /// </summary>
    /// <returns><c>true</c> if the Rock installation is in a clean state; otherwise <c>false</c>.</returns>
    public bool IsRockClean()
    {
        var items = GetRockFileStatuses();

        if ( items == null )
        {
            var rockDllPath = Path.Combine( _environmentDirectory, "Rock", "RockWeb", "Bin", "Rock.dll" );

            // If the rock install log is missing and there is no Rock.dll, then
            // assume we are clean.
            return !File.Exists( rockDllPath );
        }

        return items != null
            && items.All( f => f.IsUpToDate );
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

    /// <summary>
    /// Clones a remote repository into the environment.
    /// </summary>
    /// <param name="remoteUrl">The URL of the remote repository.</param>
    /// <param name="relativeDirectory">The relative path to the environment root.</param>
    /// <param name="branch">If specified the name of the remote branch to clone; otherwise the default branch will be cloned.</param>
    /// <param name="progress">An optional progress reporter for the clone progress.</param>
    public void Clone( string remoteUrl, string relativeDirectory, string? branch, IProgress<double>? progress )
    {
        var destinationDirectory = Path.Combine( _environmentDirectory, relativeDirectory );

        Repository.Clone( remoteUrl, destinationDirectory, new CloneOptions
        {
            BranchName = !string.IsNullOrEmpty( branch ) ? branch : null,
            OnCheckoutProgress = ( a, b, c ) =>
            {
                progress?.Report( 0.5 + ( b / ( double ) c / 2.0 ) );
            },
            FetchOptions =
            {
                CredentialsProvider = GetCredentials,
                OnTransferProgress = ( transferProgress ) =>
                {
                    progress?.Report( transferProgress.ReceivedObjects / ( double ) transferProgress.TotalObjects / 2.0 );
                    return true;
                }
            }
        } );
    }

    /// <summary>
    /// Gets the credentials from the native git implementation for the repository.
    /// </summary>
    /// <param name="repoUrl">The URL of the repository that needs authentication.</param>
    /// <param name="usernameFromUrl">The username to get credentials for.</param>
    /// <param name="supportedTypes">The supported authentication types.</param>
    /// <returns>A set of credentials to authenticate with.</returns>
    /// <exception cref="NoCredentialsException">Thrown if no credentials are available.</exception>
    private static UsernamePasswordCredentials GetCredentials( string repoUrl, string usernameFromUrl, SupportedCredentialTypes supportedTypes )
    {
        var uri = new Uri( repoUrl );
        string? username = null;
        string? password = null;

        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git.exe",
                    Arguments = "credential-manager get",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            proc.StandardInput.WriteLine( $"protocol={uri.Scheme}" );
            proc.StandardInput.WriteLine( $"host={uri.Host}" );

            if ( !string.IsNullOrEmpty( usernameFromUrl ) )
            {
                proc.StandardInput.WriteLine( $"username={usernameFromUrl}" );
            }

            proc.StandardInput.WriteLine();

            while ( !proc.StandardOutput.EndOfStream )
            {
                var line = proc.StandardOutput.ReadLine();

                if ( line?.StartsWith( "username=" ) == true )
                {
                    username = line.Substring( 9 );
                }
                else if ( line?.StartsWith( "password=" ) == true )
                {
                    password = line.Substring( 9 );
                }
            }

            proc.WaitForExit();
        }
        catch
        {
            username = null;
            password = null;
        }

        if ( username != null && password != null )
        {
            return new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            };
        }

        throw new NoCredentialsException();
    }

    /// <summary>
    /// Calculates the SHA1 hash of a files contents and returns that hash as a
    /// hexadecimal string without spacing or "0x" prefix.
    /// </summary>
    /// <param name="filename">The path to the filename to read.</param>
    /// <returns>A SHA1 hash in hexadecimal notation.</returns>
    private static string CalculateHexHash( string filename )
    {
        var stream = File.OpenRead( filename );

        return CalculateHexHash( stream );
    }

    /// <summary>
    /// Calculates the SHA1 hash of a stream and returns that hash as a
    /// hexadecimal string without spacing or "0x" prefix.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>A SHA1 hash in hexadecimal notation.</returns>
    private static string CalculateHexHash( Stream stream )
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash( stream );

        return Convert.ToHexString( hash );
    }
}
