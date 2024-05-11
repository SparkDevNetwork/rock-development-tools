using System.Text.Json;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands;

/// <summary>
/// Base handler for commands that modify content.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
abstract class BaseModifyCommandHandler<TOptions> : BaseActionCommandHandler<TOptions>
    where TOptions : BaseModifyCommandOptions
{
    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    protected JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    /// <summary>
    /// Creates an action command handler that will modify content.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    protected BaseModifyCommandHandler( TOptions options )
        : base( options )
    {
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
        if ( Options.DryRun )
        {
            var relativePath = Path.GetRelativePath( Directory.GetCurrentDirectory(), path );

            AnsiConsole.WriteLine( $"Create {relativePath}" );
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
