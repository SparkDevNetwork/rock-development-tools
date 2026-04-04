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
        // In version 1.0.5 we added a .ignore file.
        if ( environment.Data.ToolVersion.ComparePrecedenceTo( new SemVersion( 1, 0, 5 ) ) < 0 )
        {
            if ( !CreateIgnoreFile( environment ) )
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
    /// Creates a .ignore file for the environment.
    /// </summary>
    /// <param name="environment">The environment.</param>
    /// <returns><c>true</c> if the .ignore file was created successfully; otherwise, <c>false</c>.</returns>
    private bool CreateIgnoreFile( Environment environment )
    {
        var ignorePath = _fs.Path.Combine( environment.Directory, ".ignore" );
        string content;

        if ( _fs.File.Exists( ignorePath ) )
        {
            content = _fs.File.ReadAllText( ignorePath );
        }
        else
        {
            content = "# Unignore Plugins - DO NOT MODIFY\n";
        }

        var lineEnding = content.Contains( "\r\n" ) ? "\r\n" : "\n";

        foreach ( var plugin in environment.Data.Plugins )
        {
            // If the gitignore file already contains this item then skip it. This
            // most often happens when the force option is used.
            if ( content.Split( ['\r', '\n'] ).Contains( $"!/{plugin.Path}" ) )
            {
                continue;
            }

            if ( !content.EndsWith( lineEnding ) )
            {
                content = $"{content}{lineEnding}!/{plugin.Path}{lineEnding}";
            }
            else
            {
                content = $"{content}!/{plugin.Path}{lineEnding}";
            }
        }

        if ( environment.IsDryRun )
        {
            var friendlyPath = _fs.Path.GetFriendlyPath( ignorePath );
            _console.MarkupLineInterpolated( $"Create ignore file [cyan]{friendlyPath}[/]." );
        }
        else
        {
            _fs.File.WriteAllText( ignorePath, content );
        }

        return true;
    }

    #endregion
}
