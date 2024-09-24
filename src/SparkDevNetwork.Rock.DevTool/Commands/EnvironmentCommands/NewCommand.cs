using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Text.Json;

using LibGit2Sharp;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using SparkDevNetwork.Rock.DevTool.Data;
using SparkDevNetwork.Rock.DevTool.DevEnvironment;
using SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.EnvironmentCommands;

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

    private readonly IFileSystem _fs;

    /// <summary>
    /// Creates a command that will handle creating a new development
    /// environment.
    /// </summary>
    public NewCommand( IServiceProvider serviceProvider )
        : base( "new", "Create a new development environment.", serviceProvider )
    {
        _serviceProvider = serviceProvider;
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

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
        var outputDirectory = ExecuteOptions.Output ?? _fs.Directory.GetCurrentDirectory();

        outputDirectory = _fs.Path.GetFullPath( outputDirectory );

        if ( !ValidateOutput( outputDirectory ) )
        {
            return 1;
        }

        var orgName = Console.Ask<string>( "Organization Name?" );
        var orgCode = Console.Ask<string>( "Organization Code?" );
        var rockVersionString = Console.Prompt( new TextPrompt<string>( "Rock Version?" )
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
        if ( !_fs.Directory.Exists( outputDirectory ) )
        {
            _fs.Directory.CreateDirectory( outputDirectory );
        }

        // Write the "/.gitignore" file.
        WriteFile( _fs.Path.Join( outputDirectory, ".gitignore" ),
            """
            # Environment Ignores - DO NOT MODIFY
            /Rock
            /.vs
            /packages

            """ );

        // Write the "/.vscode/extensions.json" file.
        WriteFile( _fs.Path.Join( outputDirectory, ".vscode", "extensions.json" ),
            """
            {
                "recommendations": [
                    "dbaeumer.vscode-eslint",
                    "vue.volar",
                    "orta.vscode-jest"
                ]
            }

            """ );

        // Write the "/.vscode/setings.json" file.
        WriteFile( _fs.Path.Join( outputDirectory, ".vscode", "settings.json" ),
            """
            {
                "files.associations": {
                    "*.obs": "vue"
                },
                "vue.complete.casing.props": "camel",
                "vue.complete.casing.tags": "pascal",
                "files.trimTrailingWhitespace": true,
                "html.format.wrapAttributes": "preserve-aligned",
                "html.format.wrapLineLength": 0,
                "editor.codeActionsOnSave": {
                    "source.fixAll.eslint": "explicit"
                },
                "eslint.validate": [
                    "javascript",
                    "javascriptreact",
                    "typescript",
                    "typescriptreact",
                    "vue"
                ],
                "[vue]": {
                    "editor.defaultFormatter": "Vue.volar"
                },
                "css.format.spaceAroundSelectorSeparator": true,
                "css.lint.duplicateProperties": "warning",
                "css.lint.zeroUnits": "warning",
                "css.lint.propertyIgnoredDueToDisplay": "warning",
                "dotnet.preferCSharpExtension": true
            }

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

        WriteFile( _fs.Path.Join( outputDirectory, EnvironmentData.Filename ),
            JsonSerializer.Serialize( environmentData, SerializerOptions ) );

        // Write the solution file.
        var solutionFile = _fs.Path.Join( outputDirectory, $"{orgName.Replace( " ", string.Empty )}.sln" );
        WriteFile( solutionFile, GetSolutionFileContent( rockVersion != null ) );

        Console.MarkupInterpolated( $"Initialized environment in [cyan]{outputDirectory}[/]" );
        Console.WriteLine();

        if ( rockVersion != null )
        {
            try
            {
                var environment = DevEnvironment.Environment.Open( outputDirectory, _serviceProvider );
                var rockInstallation = environment.GetRockInstallation();

                environment.IsDryRun = ExecuteOptions.DryRun;

                if ( !string.IsNullOrEmpty( ExecuteOptions.Source ) )
                {
                    rockInstallation.RockEnvironmentSourceUrl = ExecuteOptions.Source;
                }

                await rockInstallation.InstallRockVersionAsync( rockVersion );
            }
            catch ( InvalidEnvironmentException ex )
            {
                Console.WriteLine( ex.Message );
                return 1;
            }
        }

        if ( !_fs.Directory.Exists( _fs.Path.Combine( outputDirectory, ".git" ) ) )
        {
            Repository.Init( outputDirectory );
            using var repo = new Repository( outputDirectory );

            // Switch the repository to use "main" as the default branch.
            repo.Refs.Add( "HEAD", "refs/heads/main", null, true );
        }

        return 0;
    }

    /// <summary>
    /// Gets the solution file content to initialize the environment with.
    /// </summary>
    /// <param name="hasRockWeb"><c>true</c> if we have a RockWeb folder.</param>
    /// <returns>The text content to put in the solution file.</returns>
    private static string GetSolutionFileContent( bool hasRockWeb )
    {
        var sln = new Solution();

        var solutionConfiguration = sln.Global.GetOrAddSection( "SolutionConfigurationPlatforms", GlobalSection.PreSolution );
        solutionConfiguration.SetProperty( "Debug|Any CPU", "Debug|Any CPU" );
        solutionConfiguration.SetProperty( "Release|Any CPU", "Release|Any CPU" );

        var solutionProperties = sln.Global.GetOrAddSection( "SolutionProperties", GlobalSection.PreSolution );
        solutionProperties.SetProperty( "HideSolutionNode", "FALSE" );

        var extensibilityGlobals = sln.Global.GetOrAddSection( "ExtensibilityGlobals", GlobalSection.PostSolution );
        extensibilityGlobals.SetProperty( "SolutionGuid", Guid.NewGuid().ToSolution() );

        if ( hasRockWeb )
        {
            var project = sln.AddProject( "RockWeb", "Rock\\RockWeb\\", Project.ProjectTypeWebSite );
            var websiteProperties = project.GetOrAddSection( "WebsiteProperties", ProjectSection.PreProject );

            websiteProperties.SetProperty( "TargetFrameworkMoniker", ".NETFramework,Version%3D4.7.2" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.VirtualPath", "/localhost_64706" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.PhysicalPath", "Rock\\RockWeb\\" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.TargetPath", "Rock\\PrecompiledWeb\\localhost_64706\\" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.Updateable", "true" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.ForceOverwrite", "true" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.FixedNames", "false" );
            websiteProperties.SetProperty( "Debug.AspNetCompiler.Debug", "True" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.VirtualPath", "/localhost_64706" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.PhysicalPath", "Rock\\RockWeb\\" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.TargetPath", "Rock\\PrecompiledWeb\\localhost_64706\\" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.Updateable", "true" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.ForceOverwrite", "true" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.FixedNames", "false" );
            websiteProperties.SetProperty( "Release.AspNetCompiler.Debug", "False" );
            websiteProperties.SetProperty( "VWDPort", "64706" );
            websiteProperties.SetProperty( "SlnRelativePath", "Rock\\RockWeb\\" );
        }

        return new SlnWriter().WriteToString( sln );
    }

    /// <summary>
    /// Validates the output directory. This makes sure it is either empty
    /// or does not yet exist.
    /// </summary>
    /// <param name="outputDirectory">The directory the environment will be written to.</param>
    /// <returns><c>true</c> if the output directory is valid; otherwise <c>false</c>.</returns>
    private bool ValidateOutput( string outputDirectory )
    {
        if ( _fs.File.Exists( outputDirectory ) )
        {
            Console.MarkupLine( "[red]Environment directory exists as a file and cannot be replaced.[/]" );
            Console.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
            Console.WriteLine();

            return false;
        }

        if ( ExecuteOptions.Force )
        {
            return true;
        }

        if ( _fs.Directory.Exists( outputDirectory ) && _fs.Directory.EnumerateFiles( outputDirectory ).Any() )
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
