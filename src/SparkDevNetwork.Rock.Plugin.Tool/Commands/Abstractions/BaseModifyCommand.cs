using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Abstractions;

/// <summary>
/// Base implementation for commands that create or modify content and files.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseModifyCommand<TOptions> : BaseActionCommand<TOptions>
    where TOptions : BaseModifyCommandOptions, new()
{
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

    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    protected JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true
    };

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
    protected override TOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.DryRun = context.ParseResult.GetValueForOption( _dryRunOption );
        options.Force = context.ParseResult.GetValueForOption( _forceOption );

        return options;
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
        if ( ExecuteOptions.DryRun )
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

        if ( !ExecuteOptions.DryRun )
        {
            _fs.Directory.CreateDirectory( path );
        }
    }
}
