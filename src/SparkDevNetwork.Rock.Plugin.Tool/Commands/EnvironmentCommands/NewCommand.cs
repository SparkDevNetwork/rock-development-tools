using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

using LibGit2Sharp;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.EnvironmentCommands;

/// <summary>
/// Container for sub-commands related to working with development environments.
/// </summary>
class NewCommand : Abstractions.BaseModifyCommand<NewCommandOptions>
{
    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// The base URL when downloading environment files.
    /// </summary>
    private readonly Option<string?> _sourceOption;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a command that will handle creating a new development
    /// environment.
    /// </summary>
    public NewCommand( IServiceProvider serviceProvider )
        : base( "new", "Create a new development environment.", serviceProvider )
    {
        _serviceProvider = serviceProvider;

        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );

        _sourceOption = new Option<string?>( "--source", "The base URL to use when downloading environment files." );

        AddOption( _outputOption );
        AddOption( _sourceOption );
    }

    /// <inheritdoc/>
    protected override NewCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Output = context.ParseResult.GetValueForOption( _outputOption );
        options.Source = context.ParseResult.GetValueForOption( _sourceOption );

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var outputDirectory = ExecuteOptions.Output ?? System.Environment.CurrentDirectory;

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
        var environmentData = new EnvironmentData
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
            JsonSerializer.Serialize( environmentData, SerializerOptions ) );

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

        if ( rockVersion != null )
        {
            var environment = Environment.Open( outputDirectory, _serviceProvider );

            if ( environment is null )
            {
                return 1;
            }

            environment.IsDryRun = ExecuteOptions.DryRun;

            if ( !string.IsNullOrEmpty( ExecuteOptions.Source ) )
            {
                environment.RockEnvironmentSourceUrl = ExecuteOptions.Source;
            }

            await environment.InstallRockVersionAsync( rockVersion );
        }

        if ( !Directory.Exists( Path.Combine( outputDirectory, ".git" ) ) )
        {
            Repository.Init( Path.Combine( outputDirectory ) );
            using var repo = new Repository( outputDirectory );

            // Switch the repository to use "main" as the default branch.
            repo.Refs.Add( "HEAD", "refs/heads/main", null, true );
        }

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

        if ( ExecuteOptions.Force )
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
