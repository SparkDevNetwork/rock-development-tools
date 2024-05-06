using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;

using Microsoft.Extensions.Logging;

using SparkDevNetwork.Rock.Plugin.Tool.CommandOptions.Environment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class NewCommand : ModifyCommandBase<NewCommandOptions>
{
    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// Creates a command that will handle creating a new development
    /// environment.
    /// </summary>
    public NewCommand()
        : base( "new", "Create a new development environment." )
    {
        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );

        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync( NewCommandOptions options )
    {
        var outputDirectory = options.Output ?? System.Environment.CurrentDirectory;

        outputDirectory = Path.GetFullPath( outputDirectory );

        if ( !ValidateOutput( outputDirectory, options.Force ) )
        {
            return Task.FromResult( 0 );
        }

        return Task.FromResult( 0 );
    }

    /// <inheritdoc/>
    protected override NewCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Output = context.ParseResult.GetValueForOption( _outputOption );

        return options;
    }

    /// <summary>
    /// Validates the output directory. This makes sure it is either empty
    /// or does not yet exist.
    /// </summary>
    /// <param name="outputDirectory">The directory the environment will be written to.</param>
    /// <param name="force">If the force command was specified.</param>
    /// <returns><c>true</c> if the output directory is valid; otherwise <c>false</c>.</returns>
    private bool ValidateOutput( string outputDirectory, bool force )
    {
        if ( File.Exists( outputDirectory ) )
        {
            Console.MarkupLine( "[red]Environment directory exists as a file and cannot be replaced.[/]" );
            Console.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
            Console.WriteLine();

            return false;
        }

        if ( force )
        {
            return true;
        }

        if ( Directory.Exists( outputDirectory ) && Directory.EnumerateFiles( outputDirectory ).Any() )
        {
            Console.MarkupLine( "[red]Environment directory is not empty and might overwrite existing files.[/]" );
            Console.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
            Console.WriteLine();
            Console.WriteLine( "To create the environment anyway, run the command with '--force' option." );
            Console.WriteLine();

            return false;
        }

        return true;
    }
}
