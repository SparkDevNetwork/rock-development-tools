using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Abstractions;

/// <summary>
/// Base implementation for commands that create or modify content and files.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseModifyCommand<TOptions> : BaseActionCommand<TOptions>
    where TOptions : BaseModifyCommandOptions, new()
{
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
            var relativePath = Path.GetRelativePath( Directory.GetCurrentDirectory(), path );

            Console.WriteLine( $"Create {relativePath}" );
        }
        else
        {
            var directory = Path.GetDirectoryName( path );

            if ( !string.IsNullOrEmpty( directory ) && !Directory.Exists( directory ) )
            {
                Directory.CreateDirectory( directory );
            }

            File.WriteAllText( path, content );
        }
    }
}
