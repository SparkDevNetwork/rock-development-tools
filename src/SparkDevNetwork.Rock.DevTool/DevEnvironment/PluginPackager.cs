using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

using GlobExpressions;

using Semver;

using SparkDevNetwork.Rock.DevTool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Handles packaging up a plugin into a Rock plugin archive file.
/// </summary>
class PluginPackager
{
    #region Fields

    /// <summary>
    /// The object that will handle access to the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The console to use for reading and writing to the terminal.
    /// </summary>
    private readonly IAnsiConsole _console;

    /// <summary>
    /// The options to use when writing JSON data to files.
    /// </summary>
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of the <see cref="PluginPackager"/> with the
    /// default options.
    /// </summary>
    /// <param name="fs">The object to use when accessing the filesystem.</param>
    /// <param name="console">The object to use when writing status information to the console.</param>
    public PluginPackager( IFileSystem fs, IAnsiConsole console )
    {
        _fs = fs;
        _console = console;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Imports an existing plugin archive into the lock file. This should be
    /// used to initialize a new lock file when an existing WebForms plugin is
    /// being updated to the new project format.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin.</param>
    /// <param name="archivePath">The path to the plugin archive file to be imported.</param>
    /// <param name="pluginVersion">The version of the plugin archive to import.</param>
    /// <returns><c>true</c> if the import was successful; otherwise <c>false</c>.</returns>
    public bool ImportPlugin( string pluginPath, string archivePath, SemVersion pluginVersion )
    {
        // Make sure the path specifies a valid plugin path.
        var plugin = LoadPluginFile( pluginPath );

        if ( plugin is null )
        {
            return false;
        }

        // Get the existing lock file information that records previous runs.
        var pluginLock = LoadPluginLockFile( pluginPath );

        if ( pluginLock is null )
        {
            return false;
        }

        if ( !_fs.File.Exists( archivePath ) )
        {
            _console.MarkupLineInterpolated( $"[red]Archive {archivePath} not found.[/]" );
            return false;
        }

        _console.WriteLine( $"Importing {plugin.Name} version {pluginVersion}." );

        using var stream = _fs.File.OpenRead( archivePath );
        using var archive = new ZipArchive( stream, ZipArchiveMode.Read );
        var installedFiles = new List<string>();

        // Find all entries in the archive that are in the "content/" folder
        // and add them to the list of files installed by this archive.
        foreach ( var entry in archive.Entries )
        {
            if ( !entry.FullName.StartsWith( "content/" ) )
            {
                continue;
            }

            var installedFile = entry.FullName[8..];
            installedFiles.Add( installedFile );
        }

        installedFiles = installedFiles.OrderBy( f => f ).ToList();

        _console.WriteLine( "Installed files:" );
        foreach ( var file in installedFiles )
        {
            _console.WriteLine( $"  {file}" );
        }

        AddReleaseToLockFile( pluginPath, pluginLock, pluginVersion, installedFiles );

        return true;
    }

    /// <summary>
    /// Creates a plugin archive and updates the lock file with the information
    /// for this packaged version.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin directory.</param>
    /// <returns><c>true</c> if the import was successful; otherwise <c>false</c>.</returns>
    public bool CreateArchive( string pluginPath )
    {
        // Make sure the path specifies a valid plugin path.
        var plugin = LoadPluginFile( pluginPath );

        if ( plugin is null )
        {
            return false;
        }

        // Make sure we have a version number.
        var pluginVersion = plugin.Version;

        if ( pluginVersion is null )
        {
            _console.MarkupLineInterpolated( $"[red]Version number must be specified in {PluginData.Filename}.[/]" );
            return false;
        }

        // Get the existing lock file information that records previous runs.
        var pluginLock = LoadPluginLockFile( pluginPath );

        if ( pluginLock is null )
        {
            return false;
        }

        if ( pluginLock.Versions.Any( v => v.Version.ComparePrecedenceTo( pluginVersion ) > 0 ) )
        {
            _console.MarkupLine( "[red]Lock file contains a later release, unable to package plugin.[/]" );
            return false;
        }

        _console.WriteLine( $"Packaging {plugin.Name} version {pluginVersion}." );

        // Find the previous version that was created.
        var previousLockVersion = pluginLock.Versions
            .Where( v => v.Version.ComparePrecedenceTo( pluginVersion ) < 0 )
            .OrderByDescending( v => v.Version )
            .FirstOrDefault();

        try
        {
            var includedFiles = GetIncludedFiles( pluginPath, plugin );
            var deletedFiles = GetDeletedFiles( plugin, pluginLock, pluginVersion, includedFiles );

            if ( includedFiles.Count == 0 )
            {
                _console.MarkupLine( $"[red]No files were found to include in archive.[/]" );
                return false;
            }

            // Display some information on the console to provide context for
            // what we are packaging.
            _console.WriteLine( "Installing files during install:" );
            foreach ( var file in includedFiles )
            {
                _console.WriteLine( $"  {file.Target}" );
            }

            if ( deletedFiles.Count > 0 )
            {
                _console.WriteLine( "Files to be deleted during install:" );
                foreach ( var file in deletedFiles )
                {
                    _console.WriteLine( $"  {file}" );
                }
            }

            var pluginSlugName = Regex.Replace( plugin.Name.ToLower(), "[^a-zA-Z0-9_-]", "-" );
            var pluginFilename = $"{pluginSlugName}-{pluginVersion}.plugin";
            var archivePath = _fs.Path.Combine( pluginPath, pluginFilename );

            WritePluginArchive( archivePath, includedFiles, deletedFiles );

            AddReleaseToLockFile( pluginPath,
                pluginLock,
                pluginVersion,
                includedFiles.Select( f => f.Target ).ToList() );

            var relativeArchivePath = _fs.Path.GetRelativePath( _fs.Directory.GetCurrentDirectory(), archivePath );
            _console.WriteLine( $"Created {relativeArchivePath}." );
        }
        catch ( DroppedException )
        {
            // This indicates an error that has already been reported.
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads the plugin data from the specified plugin path.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin directory.</param>
    /// <returns>An instance of <see cref="PluginData"/> if it was loaded; otherwise <c>null</c>.</returns>
    private PluginData? LoadPluginFile( string pluginPath )
    {
        var pluginDataPath = _fs.Path.Combine( pluginPath, PluginData.Filename );

        if ( !_fs.File.Exists( pluginDataPath ) )
        {
            _console.MarkupLineInterpolated( $"[red]No {PluginData.Filename} file found.[/]" );
            return null;
        }

        var pluginDataContent = _fs.File.ReadAllText( pluginDataPath );
        var plugin = JsonSerializer.Deserialize<PluginData>( pluginDataContent );

        if ( plugin is null )
        {
            _console.MarkupLine( "[red]Unable to parse contents of plugin file.[/]" );
            return null;
        }

        return plugin;
    }

    /// <summary>
    /// Loads the plugin lock data from the specified plugin path. If the lock
    /// file contains a version later than <paramref name="pluginVersion"/> then
    /// an error is displayed and <c>null</c> is returned.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin directory.</param>
    /// <returns>An instance of <see cref="PluginLockData"/> if it was loaded; otherwise <c>null</c>.</returns>
    private PluginLockData? LoadPluginLockFile( string pluginPath )
    {
        var pluginLockPath = _fs.Path.Combine( pluginPath, PluginLockData.Filename );

        if ( _fs.File.Exists( pluginLockPath ) )
        {
            var pluginLockContent = _fs.File.ReadAllText( pluginLockPath );
            var pluginLock = JsonSerializer.Deserialize<PluginLockData>( pluginLockContent );

            if ( pluginLock is null )
            {
                _console.MarkupLine( "[red]Unable to parse contents of plugin lock file.[/]" );
                return null;
            }

            if ( pluginLock.LockFileVersion > PluginLockData.CurrentLockFileVersion )
            {
                _console.MarkupLineInterpolated( $"[red]This tool only understands version {PluginLockData.CurrentLockFileVersion} lock files.[/]" );
                return null;
            }

            return pluginLock;
        }
        else
        {
            return new PluginLockData();
        }

    }

    /// <summary>
    /// Get a list of all source files that should be added to the package
    /// archive.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin directory.</param>
    /// <param name="plugin">The data from the plugin's JSON file.</param>
    /// <returns>A set of <see cref="SourceFile"/> objects that represent the files to be archived.</returns>
    private List<SourceFile> GetIncludedFiles( string pluginPath, PluginData plugin )
    {
        var includedFiles = new List<SourceFile>();
        var excludedFiles = new List<string>();

        if ( plugin.Include is null )
        {
            return includedFiles;
        }

        // Find all files that are explicitly excluded from the package.
        foreach ( var key in plugin.Exclude ?? [] )
        {
            var files = Glob.Files( pluginPath, key );

            foreach ( var file in files )
            {
                var relativeSource = _fs.Path.NormalizePathSeperator( file );
                var absolutePath = _fs.Path.GetFullPath( relativeSource, pluginPath );

                excludedFiles.Add( absolutePath );
            }
        }

        // Find all files that have been requested to be packaged.
        foreach ( var key in plugin.Include.Keys )
        {
            if ( key.Contains( '*' ) )
            {
                // The path is a glob path. Determine the base path before any
                // globbing starts so we can reconstruct the relative path in
                // the archive later.
                var basePath = key[..key.IndexOf( '*' )];
                var absoluteBasePath = _fs.Path.GetFullPath( basePath, pluginPath );

                var files = Glob.Files( pluginPath, key );

                // Add each file found to the list of included files.
                foreach ( var file in files )
                {
                    var relativeSource = _fs.Path.NormalizePathSeperator( file );
                    var absolutePath = _fs.Path.GetFullPath( relativeSource, pluginPath );

                    // Skip if it was excluded.
                    if ( excludedFiles.Contains( absolutePath ) )
                    {
                        continue;
                    }

                    var targetPath = _fs.Path.GetRelativePath( absoluteBasePath, absolutePath );

                    targetPath = _fs.Path.Combine( plugin.Include[key], targetPath )
                        .Replace( '\\', '/' );

                    includedFiles.Add( new SourceFile( absolutePath, targetPath ) );
                }
            }
            else
            {
                // This is a single file reference that should be added.
                var relativeSource = _fs.Path.NormalizePathSeperator( key );
                var absolutePath = _fs.Path.GetFullPath( relativeSource, pluginPath );

                if ( _fs.Directory.Exists( absolutePath ) )
                {
                    _console.MarkupLineInterpolated( $"[red]{key} appears to refer to a directory which is not supported.[/]" );
                    throw new DroppedException();
                }

                // Skip the file if it is being excluded.
                if ( excludedFiles.Contains( absolutePath ) )
                {
                    continue;
                }

                if ( !_fs.File.Exists( absolutePath ) )
                {
                    _console.MarkupLineInterpolated( $"[red]File {key} was not found.[/]" );
                    throw new DroppedException();
                }

                var targetPath = plugin.Include[key].Replace( '\\', '/' );

                if ( targetPath.EndsWith( '/' ) )
                {
                    targetPath = $"{targetPath}{_fs.Path.GetFileName( absolutePath )}";
                }

                includedFiles.Add( new SourceFile( absolutePath, targetPath ) );
            }
        }

        return includedFiles
            .DistinctBy( f => f.Target )
            .OrderBy( f => f.Target )
            .ToList();
    }

    /// <summary>
    /// Gets the files to be deleted when this plugin is installed.
    /// </summary>
    /// <param name="plugin">The data from the plugin's JSON file.</param>
    /// <param name="previousVersion">The previous version of the install archive data.</param>
    /// <param name="installedFiles">The list of files that will be installed in this archive.</param>
    /// <returns>A list of files relative to the RockWeb folder that should be deleted.</returns>
    private static List<string> GetDeletedFiles( PluginData plugin, PluginLockData pluginLock, SemVersion pluginVersion, List<SourceFile> installedFiles )
    {
        var deletedFiles = new List<string>();

        // Check for any files that were explicitely marked for deletion.
        if ( plugin.Delete is not null )
        {
            deletedFiles.AddRange( plugin.Delete.Select( f => f.Replace( '\\', '/' ) ) );
        }

        // If there are any previous versions, find all files installed by one
        // of those versions that is not installed by this version. This allows
        // for versions to be skipped and all files correctly removed.
        foreach ( var previousVersion in pluginLock.Versions )
        {
            if ( previousVersion.Version.ComparePrecedenceTo( pluginVersion ) >= 0 )
            {
                continue;
            }

            var previousFilesToDelete = previousVersion.InstalledFiles
                .Where( f => !installedFiles.Any( i => i.Target == f ) );

            deletedFiles.AddRange( previousFilesToDelete );
        }

        return deletedFiles.OrderBy( f => f ).ToList();
    }

    /// <summary>
    /// Writes the plugin archive (zipfile) by compressing the source files
    /// into the archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive file to create.</param>
    /// <param name="installedFiles">The files to be installed with the plugin.</param>
    /// <param name="deletedFiles">The files to be deleted when the plugin is installed.</param>
    private void WritePluginArchive( string archivePath, List<SourceFile> installedFiles, List<string> deletedFiles )
    {
        using var stream = _fs.File.Open( archivePath, FileMode.Create );
        using var archive = new ZipArchive( stream, ZipArchiveMode.Create );

        foreach ( var sourceFile in installedFiles )
        {
            var target = $"content/{sourceFile.Target}";

            archive.CreateEntryFromFile( sourceFile.Source, target );
        }

        if ( deletedFiles.Count > 0 )
        {
            var installFileEntry = archive.CreateEntry( "install/deletefile.lst" );
            using var installFileStream = installFileEntry.Open();
            using var installFileWriter = new StreamWriter( installFileStream );
            installFileWriter.Write( string.Join( System.Environment.NewLine, deletedFiles ) );
        }

        var uninstallFileEntry = archive.CreateEntry( "uninstall/deletefile.lst" );
        using var uninstallFileStream = uninstallFileEntry.Open();
        var filenames = installedFiles.Select( f => f.Target );
        using var uninstallFileWriter = new StreamWriter( uninstallFileStream );
        uninstallFileWriter.Write( string.Join( System.Environment.NewLine, filenames ) );
    }

    /// <summary>
    /// Adds a new release to the lock file.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin.</param>
    /// <param name="pluginLock">The existing lock file data.</param>
    /// <param name="version">The new version to write.</param>
    /// <param name="installedFiles">The files installed with this version.</param>
    private void AddReleaseToLockFile( string pluginPath, PluginLockData pluginLock, SemVersion version, List<string> installedFiles )
    {
        pluginLock.Versions.RemoveAll( v => v.Version == version );

        pluginLock.Versions.Add( new PluginLockVersionData
        {
            Version = version,
            InstalledFiles = installedFiles
        } );

        pluginLock.Versions = pluginLock.Versions
            .OrderBy( v => v.Version )
            .ToList();

        var pluginLockPath = _fs.Path.Combine( pluginPath, PluginLockData.Filename );
        var pluginLockContent = JsonSerializer.Serialize( pluginLock, _serializerOptions );

        _fs.File.WriteAllText( pluginLockPath, pluginLockContent );
    }

    #endregion

    #region Support Classes

    /// <summary>
    /// Helper class to hold the source file path and the target path it will
    /// be installed to.
    /// </summary>
    private class SourceFile
    {
        /// <summary>
        /// The absolute path to the source file on the local machine.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// The target path relative to the RockWeb folder when installing. This
        /// should not be prefixed with a forward slash.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SourceFile"/>.
        /// </summary>
        /// <param name="source">The absolute path to the source file.</param>
        /// <param name="target">The target path relative to the RockWeb folder.</param>
        public SourceFile( string source, string target )
        {
            Source = source;
            Target = target;
        }
    }

    /// <summary>
    /// Simple exception that will be used to indicate that an error has
    /// already been displayed and a failed result should be returned.
    /// </summary>
    private class DroppedException : Exception
    {
    }

    #endregion
}
