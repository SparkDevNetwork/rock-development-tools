using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

namespace SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

/// <summary>
/// Imports a previously packed plugin into the lock file.
/// </summary>
class ImportCommand : Abstractions.BaseModifyCommand
{
    #region Fields

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The option that defines the directory of the plugin.
    /// </summary>
    private readonly Option<string?> _pathOption;

    /// <summary>
    /// The argument that defines the path to the archive to import.
    /// </summary>
    private readonly Argument<string> _archiveArgument;

    /// <summary>
    /// The argument that defines the version number of the archive.
    /// </summary>
    private readonly Argument<SemVersion> _versionArgument;

    #endregion

    #region Properties

    /// <summary>
    /// The relative path to the plugin.
    /// </summary>
    public string PluginPath { get; set; } = null!;

    /// <summary>
    /// The path to the archive to be imported.
    /// </summary>
    public string Archive { get; set; } = null!;

    /// <summary>
    /// The version number of the archive to be imported.
    /// </summary>
    public SemVersion Version { get; set; } = null!;

    #endregion

    /// <summary>
    /// Creates a command that will handle importing an existing archive.
    /// </summary>
    public ImportCommand( IServiceProvider serviceProvider )
        : base( "import", "Imports an existing plugin archive.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _pathOption = new Option<string?>( "--path", "The path to the plugin if not the working directory." );
        _archiveArgument = new Argument<string>( "archive", "The archive to be imported." );
        _versionArgument = new Argument<SemVersion>( "version", ParseVersionArgument, description: "The version of the archive to import." );

        AddOption( _pathOption );
        AddArgument( _archiveArgument );
        AddArgument( _versionArgument );
    }

    /// <inheritdoc/>
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        PluginPath = context.ParseResult.GetValueForOption( _pathOption )
            ?? _fs.Directory.GetCurrentDirectory();
        Archive = context.ParseResult.GetValueForArgument( _archiveArgument );
        Version = context.ParseResult.GetValueForArgument( _versionArgument )!;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var packager = new PluginPackager( _fs, Console );

        if ( !packager.ImportPlugin( PluginPath, Archive, Version ) )
        {
            return Task.FromResult( 1 );
        }

        return Task.FromResult( 0 );
    }

    private static SemVersion ParseVersionArgument( ArgumentResult result )
    {
        if ( SemVersion.TryParse( result.Tokens[0].Value, SemVersionStyles.Strict, out var version ) )
        {
            return version;
        }
        else
        {
            result.ErrorMessage = "Invalid version number.";
            return new SemVersion( 0 );
        }
    }
}
