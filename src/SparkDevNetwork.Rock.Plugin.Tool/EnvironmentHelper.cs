using System.IO.Compression;

using Semver;

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
}
