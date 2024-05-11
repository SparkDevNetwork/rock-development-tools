using System.Text.Json;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Environment;

/// <summary>
/// Command handler for <see cref="NewCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class NewCommandHandler : Abstractions.BaseModifyCommandHandler<NewCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    public NewCommandHandler( NewCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override async Task<int> InvokeAsync()
    {
        var outputDirectory = Options.Output ?? System.Environment.CurrentDirectory;

        outputDirectory = Path.GetFullPath( outputDirectory );

        if ( !ValidateOutput( outputDirectory ) )
        {
            return 1;
        }

        var orgName = AnsiConsole.Ask<string>( "Organization Name?" );
        var orgCode = AnsiConsole.Ask<string>( "Organization Code?" );
        var rockVersionString = AnsiConsole.Prompt( new TextPrompt<string>( "Rock Version?" )
        {
            AllowEmpty = true,
            Validator = ValidateRockVersionPrompt
        } );

        var rockVersion = !string.IsNullOrEmpty( rockVersionString )
            ? SemVersion.Parse( rockVersionString, SemVersionStyles.Strict )
            : null;

        return await CreateEnvironment( outputDirectory, orgName, orgCode, rockVersion );
    }

    /// <summary>
    /// Creates the environment with the specified values.
    /// </summary>
    /// <param name="outputDirectory">The directory to create the environment in.</param>
    /// <param name="orgName">The name of the organization.</param>
    /// <param name="orgCode">The organization code used in namespaces and tables.</param>
    /// <param name="rockVersion">The version of Rock to install in the environment.</param>
    /// <returns>A task that indicates when the operation has completed.</returns>
    private async Task<int> CreateEnvironment( string outputDirectory, string orgName, string orgCode, SemVersion? rockVersion )
    {
        if ( !Directory.Exists( outputDirectory ) )
        {
            Directory.CreateDirectory( outputDirectory );
        }

        // Write the "/.gitignore" file.
        WriteFile( Path.Join( outputDirectory, ".gitignore" ),
            """
            # Do not make any changes below this line.
            /Rock
            """ );

        // Write the environment JSON file.
        var environment = new EnvironmentData
        {
            Organization = new OrganizationData
            {
                Name = orgName,
                Code = orgCode
            },
            Rock = new RockData
            {
                Version = rockVersion != null ? rockVersion.ToString() : ""
            }
        };

        WriteFile( Path.Join( outputDirectory, EnvironmentData.Filename ),
            JsonSerializer.Serialize( environment, SerializerOptions ) );

        // Write the solution file.
        WriteFile( Path.Join( outputDirectory, $"{orgName.Replace( " ", string.Empty )}.sln" ),
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Global
                GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                    Release|Any CPU = Release|Any CPU
                EndGlobalSection
                GlobalSection(SolutionProperties) = preSolution
                    HideSolutionNode = FALSE
                EndGlobalSection
            EndGlobal
            """ );

        AnsiConsole.MarkupInterpolated( $"Initialized environment in [cyan]{outputDirectory}[/]" );
        AnsiConsole.WriteLine();

        return 0;
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

    /// <summary>
    /// Validates the Rock version text entered on the prompt.
    /// </summary>
    /// <param name="value">The value entered on the prompt.</param>
    /// <returns>A validation result object that indicates if the value is valid.</returns>
    private ValidationResult ValidateRockVersionPrompt( string value )
    {
        if ( value.Length == 0 )
        {
            return ValidationResult.Success();
        }
        else if ( SemVersion.TryParse( value, SemVersionStyles.Strict, out _ ) )
        {
            return ValidationResult.Success();
        }
        else
        {
            return ValidationResult.Error( "Invalid version number." );
        }
    }
}
