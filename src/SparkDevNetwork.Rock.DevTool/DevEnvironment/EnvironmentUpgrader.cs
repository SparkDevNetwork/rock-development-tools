using System.IO.Abstractions;
using System.Text.Json;

using Semver;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.DevEnvironment;

/// <summary>
/// Handles upgrading an environment to the latest tool version.
/// </summary>
class EnvironmentUpgrader
{
    #region Fields

    /// <summary>
    /// The file system abstraction.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The console for writing messages to the console.
    /// </summary>
    private readonly IAnsiConsole _console;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentUpgrader"/> class.
    /// </summary>
    /// <param name="fs">The file system abstraction.</param>
    /// <param name="console">The console for writing messages to the console.</param>
    public EnvironmentUpgrader( IFileSystem fs, IAnsiConsole console )
    {
        _fs = fs;
        _console = console;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Upgrades the specified environment to the latest tool version.
    /// </summary>
    /// <param name="environment">The environment to upgrade.</param>
    /// <returns><c>true</c> if the upgrade was successful; otherwise, <c>false</c>.</returns>
    public bool Upgrade( Environment environment )
    {
        // In version 1.0.5 we switched to a VS Code workspace file.
        if ( environment.Data.ToolVersion.ComparePrecedenceTo( new SemVersion( 1, 0, 5 ) ) < 0 )
        {
            if ( !CreateCodeWorkspaceFile( environment ) )
            {
                return false;
            }

            environment.Data.ToolVersion = new SemVersion( 1, 0, 5 );
            environment.Save();
        }

        // Set the final tool version to the current version.
        environment.Data.ToolVersion = Support.CurrentToolVersion;

        environment.Save();

        return true;
    }

    /// <summary>
    /// Creates a VS Code workspace file for the environment.
    /// </summary>
    /// <param name="environment">The environment.</param>
    /// <returns><c>true</c> if the workspace file was created successfully; otherwise, <c>false</c>.</returns>
    private bool CreateCodeWorkspaceFile( Environment environment )
    {
        if ( environment.Data.Organization.Name == null )
        {
            _console.MarkupLine( "[red]Organization Name is not defined in the environment, cannot create workspace file.[/]" );
            return false;
        }

        var orgNameForFile = environment.Data.Organization.Name.Replace( " ", string.Empty );
        var workspaceFile = _fs.Path.Combine( environment.Directory, $"{orgNameForFile}.code-workspace" );
        var settingsFile = _fs.Path.Combine( environment.Directory, ".vscode", "settings.json" );
        var extensionsFile = _fs.Path.Combine( environment.Directory, ".vscode", "extensions.json" );
        if ( _fs.File.Exists( workspaceFile ) )
        {
            _console.MarkupLine( "[yellow]Workspace file already exists, skipping creation.[/]" );
            return true;
        }

        var workspaceData = new Dictionary<string, object>();

        // Add all the plugins as workspace folders.
        foreach ( var plugin in environment.Data.Plugins.OrderBy( p => p.Path ) )
        {
            if ( !workspaceData.TryGetValue( "folders", out var foldersObject ) )
            {
                foldersObject = new List<object>();
                workspaceData["folders"] = foldersObject;
            }

            var folders = ( List<object> ) foldersObject;

            folders.Add( new Dictionary<string, object>
            {
                ["path"] = plugin.Path,
            } );
        }

        // Add the existing settings if the file exists.
        if ( _fs.File.Exists( settingsFile ) )
        {
            var settingsJson = _fs.File.ReadAllText( settingsFile );
            var settingsData = JsonSerializer.Deserialize<Dictionary<string, object>>( settingsJson )
                ?? [];

            workspaceData["settings"] = settingsData;
        }

        // Add the existing extensions if the file exists.
        if ( _fs.File.Exists( extensionsFile ) )
        {
            var extensionsJson = _fs.File.ReadAllText( extensionsFile );
            var extensionsData = JsonSerializer.Deserialize<Dictionary<string, object>>( extensionsJson )
                ?? [];

            workspaceData["extensions"] = extensionsData;
        }

        var json = JsonSerializer.Serialize( workspaceData, Support.SerializerOptions );

        if ( environment.IsDryRun )
        {
            var friendlyPath = _fs.Path.GetFriendlyPath( workspaceFile );
            _console.MarkupLineInterpolated( $"Create workspace file [cyan]{friendlyPath}[/]." );
        }
        else
        {
            _fs.File.WriteAllText( workspaceFile, json );

            if ( _fs.File.Exists( settingsFile ) )
            {
                _fs.File.Delete( settingsFile );
            }

            if ( _fs.File.Exists( extensionsFile ) )
            {
                _fs.File.Delete( extensionsFile );
            }
        }

        return true;
    }

    #endregion
}
