using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.DevTool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

class RockInstallation
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
    /// The absolute path to the Rock installation directory.
    /// </summary>
    private readonly string _rockPath;

    /// <summary>
    /// The data that describes the information about the Rock instance.
    /// </summary>
    private readonly RockData _data;

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
    /// Creates a new instance of the Rock installation handler. This object will
    /// handle anything related to file system for Rock.
    /// </summary>
    /// <param name="rockPath">The path to the Rock installation.</param>
    /// <param name="data">The data that describes the Rock installation.</param>
    /// <param name="fileSystem">The object that will provide access to the file system.</param>
    /// <param name="console">The object that will be used to write to standard output.</param>
    /// <param name="logger">The object that will log diagnostic information.</param>
    public RockInstallation( string rockPath, RockData data, IFileSystem fileSystem, IAnsiConsole console, ILogger logger )
    {
        _rockPath = rockPath;
        _data = data;
        _fs = fileSystem;
        _console = console;
        _logger = logger;
    }

    /// <summary>
    /// Installs the configured Rock binary version into the default Rock
    /// directory.
    /// </summary>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public Task InstallRockAsync()
    {
        if ( !SemVersion.TryParse( _data.Version, SemVersionStyles.Strict, out var version ) )
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

        _console.MarkupLineInterpolated( $"Installing Rock from [cyan]{url}[/]" );

        var progress = _console.Progress();

        await progress.StartAsync( async ctx =>
        {
            var downloadProgress = ctx.AddTask( "Downloading Rock", true, 1 );
            var extractProgress = ctx.AddTask( IsDryRun ? "Validating archive" : "Extracting files", false, 1 );

            using var client = new HttpClient();
            using var memoryStream = new MemoryStream();

            if ( !url.Contains( "://" ) )
            {
                await _fs.File.ReadFileAsync( url, memoryStream, p => downloadProgress.Value = p );

                return;
            }
            else
            {
                await client.DownloadAsync( url, memoryStream, p => downloadProgress.Value = p );
            }

            downloadProgress.StopTask();
            ctx.Refresh();

            // Start the extract task and then open the archive and extract.
            extractProgress.StartTask();
            memoryStream.Position = 0;

            using var archive = new ZipArchive( memoryStream );
            var hashes = ExtractAllFiles( _rockPath, archive, extractProgress );

            var rockObject = new RockInstallationData
            {
                Files = hashes
            };
            var rockJson = JsonSerializer.Serialize( rockObject, Support.SerializerOptions );
            _fs.File.WriteAllText( _fs.Path.Combine( _rockPath, ".rock.json" ), rockJson );

            extractProgress.StopTask();
            ctx.Refresh();
        } );

        _console.MarkupLineInterpolated( $"Installed Rock [cyan]{rockVersion}[/] into [cyan]{_rockPath.Replace( '/', Path.DirectorySeparatorChar )}[/]" );
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

            var destinationFilePath = _fs.Path.Combine( destinationDirectory, entry.FullName );
            var fileDirectory = _fs.Path.GetDirectoryName( destinationFilePath );

            if ( !IsDryRun )
            {
                if ( fileDirectory != null && !_fs.Directory.Exists( fileDirectory ) )
                {
                    _fs.Directory.CreateDirectory( fileDirectory );
                }

                entry.ExtractToFile( _fs.Path.Combine( destinationDirectory, entry.FullName ), true );
            }

            using var fileStream = entry.Open();
            var hash = fileStream.CalculateHexHash();

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
        var rockStatusFile = _fs.Path.Combine( _rockPath, ".rock.json" );

        if ( !_fs.File.Exists( rockStatusFile ) )
        {
            // Assume nothing to do.
            return;
        }

        var rockInstallation = JsonSerializer.Deserialize<RockInstallationData>( _fs.File.ReadAllText( rockStatusFile ) );

        if ( rockInstallation == null || rockInstallation.Files == null )
        {
            _logger.LogError( "Rock installation file {file} was not valid.", rockStatusFile );

            throw new InvalidOperationException( "Rock installation is not clean." );
        }

        var directoryPaths = new List<string>();

        foreach ( var file in rockInstallation.Files )
        {
            var filePath = _fs.Path.Combine( _rockPath, file.Key );

            // Remove the file if it exists.
            if ( _fs.File.Exists( filePath ) )
            {
                _fs.File.Delete( filePath );
            }

            var fileDirectory = _fs.Path.GetDirectoryName( filePath );

            if ( fileDirectory != null && !directoryPaths.Contains( fileDirectory ) )
            {
                directoryPaths.Add( fileDirectory );
            }
        }

        _fs.File.Delete( rockStatusFile );

        // Delete any directory that is now empty.
        foreach ( var directoryPath in directoryPaths.OrderByDescending( d => d.Length ) )
        {
            // Not every directory has a file, so we also need to check
            // parent directories to see if they are empty in case we are
            // dealing with a grand-parent directory that only had
            // sub-directories but no files.
            var path = directoryPath;

            while ( _fs.Path.GetFullPath( path ) != _fs.Path.GetFullPath( _rockPath ) )
            {
                if ( _fs.Directory.Exists( path ) )
                {
                    if ( _fs.Directory.GetFiles( path ).Length > 0 || _fs.Directory.GetDirectories( path ).Length > 0 )
                    {
                        break;
                    }

                    _fs.Directory.Delete( path );
                }

                path = _fs.Path.GetDirectoryName( path );

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
        if ( !_fs.Directory.Exists( _rockPath ) )
        {
            return;
        }

        if ( IsDryRun )
        {
            var friendlyPath = _fs.Path.GetFriendlyPath( _rockPath );

            _console.MarkupLineInterpolated( $"Remove [cyan]{friendlyPath}[/]." );
        }

        bool RemoveDirectory( string directory )
        {
            bool removeDirectory = true;

            foreach ( var dirpath in _fs.Directory.EnumerateDirectories( directory ) )
            {
                var relativePath = _fs.Path.GetRelativePath( _rockPath, dirpath );

                if ( PreservedRockFiles.Contains( relativePath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    var friendlyPath = _fs.Path.GetFriendlyPath( dirpath );

                    _logger.LogInformation( "Preserving directory {path}", friendlyPath );
                    removeDirectory = false;
                }
                else if ( !RemoveDirectory( dirpath ) )
                {
                    removeDirectory = false;
                }
            }

            foreach ( var filepath in _fs.Directory.EnumerateFiles( directory ) )
            {
                var relativePath = _fs.Path.GetRelativePath( _rockPath, filepath );

                if ( PreservedRockFiles.Contains( relativePath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    var friendlyPath = _fs.Path.GetFriendlyPath( filepath );

                    _logger.LogInformation( "Preserving file {path}", friendlyPath );
                    removeDirectory = false;
                }
                else if ( !IsDryRun )
                {
                    _fs.File.Delete( filepath );
                }
            }

            if ( removeDirectory && !IsDryRun )
            {
                _fs.Directory.Delete( directory, false );
            }

            return removeDirectory;
        }

        RemoveDirectory( _rockPath );
    }

    /// <summary>
    /// Checks if the Rock installation is up to date. This is a best guess
    /// based on the version number of the Rock.dll file.
    /// </summary>
    /// <returns>An instance of <see cref="EnvironmentStatusItem"/> that describes the status.</returns>
    public RockStatusItem GetRockStatus()
    {
        if ( _data.Version == "custom" )
        {
            return new RockStatusItem( [] );
        }

        if ( !SemVersion.TryParse( _data.Version, SemVersionStyles.Strict, out var expectedVersion ) )
        {
            _logger.LogError( "Unable to parse Rock version number '{version}'.", _data.Version );
            return new RockStatusItem( "has an invalid version number.", null );
        }

        var fileStatuses = GetRockFileStatuses();

        if ( fileStatuses != null && fileStatuses.Any( f => !f.IsUpToDate ) )
        {
            return new RockStatusItem( "has been modified since installation.", fileStatuses );
        }

        var rockDllPath = _fs.Path.Combine( _rockPath, "RockWeb", "Bin", "Rock.dll" );
        if ( !_fs.File.Exists( rockDllPath ) )
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
    /// Gets a list of status items that reflect the Rock installation status
    /// for each individual file.
    /// </summary>
    /// <returns>A list of <see cref="StatusItem"/> objects.</returns>
    public List<StatusItem>? GetRockFileStatuses()
    {
        var rockStatusFile = _fs.Path.Combine( _rockPath, ".rock.json" );

        if ( !_fs.File.Exists( rockStatusFile ) )
        {
            _logger.LogInformation( "Rock installation file {file} is missing.", rockStatusFile );
            return null;
        }

        var rockInstallation = JsonSerializer.Deserialize<RockInstallationData>( _fs.File.ReadAllText( rockStatusFile ) );

        if ( rockInstallation == null || rockInstallation.Files == null )
        {
            _logger.LogError( "Rock installation file {file} was not valid.", rockStatusFile );
            return null;
        }

        var fileStatuses = new List<StatusItem>();

        foreach ( var file in rockInstallation.Files )
        {
            // Files in App_Data are expected to change.
            if ( file.Key.StartsWith( "RockWeb/App_Data/" ) )
            {
                continue;
            }

            var filePath = file.Key.Replace( '/', Path.DirectorySeparatorChar );

            filePath = _fs.Path.Combine( _rockPath, filePath );
            var friendlyPath = _fs.Path.GetFriendlyPath( filePath );

            if ( !_fs.File.Exists( filePath ) )
            {
                fileStatuses.Add( new StatusItem( friendlyPath, "is missing." ) );
                continue;
            }

            var hash = _fs.File.CalculateHexHash( filePath );

            if ( hash != file.Value )
            {
                fileStatuses.Add( new StatusItem( friendlyPath, "Has been modified." ) );
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
            var rockDllPath = _fs.Path.Combine( _rockPath, "RockWeb", "Bin", "Rock.dll" );

            // If the rock install log is missing and there is no Rock.dll, then
            // assume we are clean.
            return !_fs.File.Exists( rockDllPath );
        }

        return items != null
            && items.All( f => f.IsUpToDate );
    }
}
