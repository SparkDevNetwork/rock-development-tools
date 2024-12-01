using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.Abstractions;

/// <summary>
/// Base implementation for commands that create or modify content and files.
/// </summary>
abstract class BaseModifyCommand : BaseActionCommand
{
    #region Fields

    /// <summary>
    /// The file system object that will handle accessing the file system. This
    /// is used so we can have a fake file system available during testing.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The option that describes if this command should be a dry-run and not
    /// actually make any modifications.
    /// </summary>
    private readonly Option<bool> _dryRunOption;

    /// <summary>
    /// The option that describes if this command should overwrite any existing
    /// data in the process of its execution.
    /// </summary>
    private readonly Option<bool> _forceOption;

    #endregion

    #region Properties

    /// <summary>
    /// Will be <c>true</c> if the command should not actually make any
    /// modifications but instead just report what would have been done.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Will be <c>true</c> if the command should overwrite anything that
    /// would otherwise cause the command to abort without making changes.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    protected JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    #endregion

    /// <summary>
    /// Creates a command that will perform some action to create or modify data.
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    public BaseModifyCommand( string name, string description, IServiceProvider serviceProvider )
        : base( name, description, serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _dryRunOption = new Option<bool>( "--dry-run", "Displays a summary of what would happen if the given command line were run." );
        _forceOption = new Option<bool>( "--force", "Forces content to be generated even if it would change existing files." );

        AddOption( _dryRunOption );
        AddOption( _forceOption );
    }

    /// <inheritdoc/>
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        DryRun = context.ParseResult.GetValueForOption( _dryRunOption );
        Force = context.ParseResult.GetValueForOption( _forceOption );
    }

    /// <summary>
    /// Creates or overwrites the file with the specified content. If the
    /// command is in dry-run mode then no changes are made and a message is
    /// displayed instead.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="content">The new content for the file.</param>
    protected void WriteFile( string path, string content )
    {
        if ( DryRun )
        {
            var relativePath = _fs.Path.GetFriendlyPath( path );

            if ( _fs.File.Exists( path ) )
            {
                Console.MarkupLineInterpolated( $"Replace [cyan]{relativePath}[/]." );
            }
            else
            {
                Console.MarkupLineInterpolated( $"Create [cyan]{relativePath}[/]." );
            }
        }
        else
        {
            var directory = _fs.Path.GetDirectoryName( path );

            if ( !string.IsNullOrEmpty( directory ) && !_fs.Directory.Exists( directory ) )
            {
                CreateDirectory( directory );
            }

            _fs.File.WriteAllText( path, content );
        }
    }

    /// <summary>
    /// Create the directory at the given path unless in a dry-run.
    /// </summary>
    /// <param name="path">The path to be created.</param>
    protected void CreateDirectory( string path )
    {
        if ( _fs.Directory.Exists( path ) )
        {
            return;
        }

        if ( !DryRun )
        {
            _fs.Directory.CreateDirectory( path );
        }
    }
}
