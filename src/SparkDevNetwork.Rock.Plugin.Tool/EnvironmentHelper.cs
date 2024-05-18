using System.IO.Compression;
using System.Reflection;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// Handles updating and installing environments.
/// </summary>
class EnvironmentHelper
{
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
    /// The source for the Rock installation archives used when downloading
    /// binary installations.
    /// </summary>
    public string RockEnvironmentSourceUrl { get; set; } = "https://rockrms.blob.core.windows.net/developer/environments";

    /// <summary>
    /// Determines if this operation is a dry-run. When <c>true</c> no changes
    /// will be made to files on disk.
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// The logger to use when writing diagnostic information.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Creates a new instance of the environment helper.
    /// </summary>
    /// <param name="logger">The logger to use when writing diagnostic messages.</param>
    public EnvironmentHelper( ILogger logger )
    {
        Logger = logger;
    }

    /// <summary>
    /// Installs the specified Rock binary version into the destination
    /// directory.
    /// </summary>
    /// <param name="destinationDirectory">The directory to install the binary release into.</param>
    /// <param name="rockVersion">The version of Rock to install.</param>
    /// <returns>A <see cref="Task"/> that indicates when the operation has completed.</returns>
    public async Task InstallRockVersion( string destinationDirectory, SemVersion rockVersion )
    {
        var url = $"{RockEnvironmentSourceUrl.TrimEnd( '/' )}/Rock-{rockVersion}.zip";

        AnsiConsole.MarkupLineInterpolated( $"Installing Rock from [cyan]{url}[/]" );

        // Remove the existing Rock installation, if any.
        RemoveRock( destinationDirectory );

        var progress = AnsiConsole.Progress();

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

        AnsiConsole.MarkupLineInterpolated( $"Installed Rock [cyan]{rockVersion}[/] into [cyan]{destinationDirectory}[/]" );
        AnsiConsole.WriteLine();
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

            AnsiConsole.WriteLine( $"Remove {relativeTargetDirectory}" );
        }

        bool RemoveDirectory( string directory )
        {
            bool removeDirectory = true;

            foreach ( var dir in Directory.EnumerateDirectories( directory ) )
            {
                var dirpath = Path.Combine( directory, dir );
                var relativepath = Path.GetRelativePath( targetDirectory, dirpath );

                if ( PreservedRockFiles.Contains( relativepath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    if ( IsDryRun )
                    {
                        AnsiConsole.WriteLine( $"  Preserve {relativepath}" );
                    }

                    removeDirectory = false;
                }
                else if ( !RemoveDirectory( Path.Combine( directory, dir ) ) )
                {
                    removeDirectory = false;
                }
            }

            foreach ( var file in Directory.EnumerateFiles( directory ) )
            {
                var filepath = Path.Combine( directory, file );
                var relativepath = Path.GetRelativePath( targetDirectory, filepath );

                if ( PreservedRockFiles.Contains( relativepath.Replace( '\\', '/' ), StringComparer.OrdinalIgnoreCase ) )
                {
                    if ( IsDryRun )
                    {
                        AnsiConsole.WriteLine( $"  Preserve {relativepath}" );
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
    /// Checks if the environment is up to date with the configuration.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="environment">The environment configuration.</param>
    /// <returns><c>true</c> if the environment is already up to date; otherwise <c>false</c>.</returns>
    public bool IsEnvironmentUpToDate( string environmentDirectory, EnvironmentData environment )
    {
        if ( !IsRockUpToDate( environmentDirectory, environment.Rock ) )
        {
            return false;
        }

        if ( environment.Plugins != null )
        {
            foreach ( var plugin in environment.Plugins )
            {
                if ( !IsPluginUpToDate( environmentDirectory, plugin ) )
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the Rock installation is up to date. This is a best guess
    /// based on the version number of the Rock.dll file.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="rock">The Rock environment configuration.</param>
    /// <returns><c>true</c> if the Rock version number is correct; otherwise <c>false</c>.</returns>
    private bool IsRockUpToDate( string environmentDirectory, RockData? rock )
    {
        if ( rock == null || rock.Version == "custom" )
        {
            return true;
        }

        if ( !SemVersion.TryParse( rock.Version, SemVersionStyles.Strict, out var version ) )
        {
            Logger.LogError( "Unable to parse Rock version number '{version}'.", rock.Version );
            return false;
        }

        var rockDllPath = Path.Combine( environmentDirectory, "Rock", "RockWeb", "Bin", "Rock.dll" );
        if ( !File.Exists( rockDllPath ) )
        {
            Logger.LogInformation( "No Rock assembly was found at {filename}.", rockDllPath );
            return false;
        }

        var asmName = AssemblyName.GetAssemblyName( rockDllPath );

        if ( asmName.Version == null )
        {
            Logger.LogError( "No version number found in Rock assembly." );
            return false;
        }

        if ( version.Major < 2 )
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor
                && version.Patch == asmName.Version.Build;

            if ( !doesVersionMatch )
            {
                Logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", version, asmName.Version );
                return false;
            }
        }
        else
        {
            var doesVersionMatch = version.Major == asmName.Version.Major
                && version.Minor == asmName.Version.Minor;

            if ( !doesVersionMatch )
            {
                Logger.LogInformation( "Rock assembly version number {rockVersion} does not match expected version {expectedVersion}.", version, asmName.Version );
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the plugin is up to date with the environment configuration.
    /// </summary>
    /// <param name="environmentDirectory">The directory that contains the environment.</param>
    /// <param name="plugin">The plugin configuration.</param>
    /// <returns><c>true</c> if the plugin branch is correct; otherwise <c>false</c>.</returns>
    private bool IsPluginUpToDate( string environmentDirectory, PluginData plugin )
    {
        if ( plugin.Path == null || plugin.Url == null )
        {
            if ( plugin.Path == null )
            {
                Logger.LogError( "Defined plugin is missing path." );
            }
            else
            {
                Logger.LogError( "Plugin {name} is missing url.", plugin.Path );
            }

            return false;
        }

        var pluginDirectory = Path.Combine( environmentDirectory, plugin.Path.Replace( '/', Path.PathSeparator ) );

        if ( !Directory.Exists( pluginDirectory ) )
        {
            Logger.LogInformation( "Plugin {path} is missing.", plugin.Path );
            return false;
        }

        if ( !Repository.IsValid( pluginDirectory ) )
        {
            Logger.LogError( "Plugin {path} is not a git repository.", plugin.Path );
            return false;
        }

        if ( plugin.Branch == null )
        {
            return true;
        }

        var repository = new Repository( pluginDirectory );
        var reference = repository.Head.Reference.TargetIdentifier;

        if ( !reference.StartsWith( "refs/heads/" ) )
        {
            Logger.LogInformation( "Plugin {path} is not on a branch.", plugin.Path );
            return false;
        }

        var branch = reference.Substring( 11 );

        if ( plugin.Branch != branch )
        {
            Logger.LogInformation( "Plugin {path} is on branch {repoBranch} instead of {expectedBranch}.", plugin.Path, branch, plugin.Branch );
            return false;
        }

        return true;
    }
}
