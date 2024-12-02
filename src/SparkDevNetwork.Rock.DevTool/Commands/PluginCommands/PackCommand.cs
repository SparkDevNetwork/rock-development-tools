using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

namespace SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

/// <summary>
/// Packs a plugin into an installation archive.
/// </summary>
class PackCommand : Abstractions.BaseModifyCommand
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

    #endregion

    #region Properties

    /// <summary>
    /// The relative path to the plugin.
    /// </summary>
    public string PluginPath { get; set; } = null!;

    #endregion

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public PackCommand( IServiceProvider serviceProvider )
        : base( "pack", "Packs a plugin into an installation archive.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _pathOption = new Option<string?>( "--path", "The path to the plugin if not the working directory." );

        AddOption( _pathOption );
    }

    /// <inheritdoc/>
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        PluginPath = context.ParseResult.GetValueForOption( _pathOption )
            ?? _fs.Directory.GetCurrentDirectory();
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var packager = new PluginPackager( _fs, Console );

        if ( !packager.CreateArchive( PluginPath ) )
        {
            return Task.FromResult( 1 );
        }

        return Task.FromResult( 0 );
    }
}
