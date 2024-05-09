using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

/// <summary>
/// Command handler for <see cref="NewCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class NewCommandHandler : BaseModifyCommandHandler<NewCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    protected NewCommandHandler( NewCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override Task<int> InvokeAsync()
    {
        var outputDirectory = Options.Output ?? System.Environment.CurrentDirectory;

        outputDirectory = Path.GetFullPath( outputDirectory );

        if ( !ValidateOutput( outputDirectory ) )
        {
            return Task.FromResult( 1 );
        }

        return Task.FromResult( 0 );
    }

    /// <summary>
    /// Validates the output directory. This makes sure it is either empty
    /// or does not yet exist.
    /// </summary>
    /// <param name="outputDirectory">The directory the environment will be written to.</param>
    /// <returns><c>true</c> if the output directory is valid; otherwise <c>false</c>.</returns>
    private bool ValidateOutput( string outputDirectory )
    {
        if ( File.Exists( outputDirectory ) )
        {
            AnsiConsole.MarkupLine( "[red]Environment directory exists as a file and cannot be replaced.[/]" );
            AnsiConsole.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
            AnsiConsole.WriteLine();

            return false;
        }

        if ( Options.Force )
        {
            return true;
        }

        if ( Directory.Exists( outputDirectory ) && Directory.EnumerateFiles( outputDirectory ).Any() )
        {
            AnsiConsole.MarkupLine( "[red]Environment directory is not empty and might overwrite existing files.[/]" );
            AnsiConsole.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine( "To create the environment anyway, run the command with '--force' option." );
            AnsiConsole.WriteLine();

            return false;
        }

        return true;
    }
}
