using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

using Fluid;

using LibGit2Sharp;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
partial class NewCommand : Abstractions.BaseModifyCommand<NewCommandOptions>
{
    private readonly IFileSystem _fs;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    private DevEnvironment.Environment _environment = null!;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public NewCommand( IServiceProvider serviceProvider )
        : base( "new", "Creates a new plugin following a standard template.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();
        _serviceProvider = serviceProvider;

        _environmentOption = new Option<string?>( "--environment", "The directory that contains the environment." );
        _environmentOption.AddAlias( "--env" );

        _outputOption = new Option<string?>( "--output", "Location to place the generated output in the environment." );
        _outputOption.AddAlias( "-o" );

        AddOption( _environmentOption );
        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override NewCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
        options.Output = context.ParseResult.GetValueForOption( _outputOption );

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var env = OpenEnvironment();

        if ( env == null )
        {
            return 1;
        }

        _environment = env;

        PopulateOptionsFromEnvironment();

        PromptForMissingOptions();

        var outputDirectory = ExecuteOptions.Output ?? _fs.Path.Combine( environmentDirectory, ExecuteOptions.PluginCode! );

        if ( !ExecuteOptions.Force )
        {
            if ( env.GetPlugins().Any( p => p.Path == ExecuteOptions.PluginCode ) )
            {
                Console.MarkupLine( "[red]Plugin already exists in the environment.[/]" );
                Console.WriteLine();
                Console.WriteLine( "To create the plugin anyway, run the command with '--force' option." );
                Console.WriteLine();

                return 1;
            }

            if ( !_fs.Directory.IsEmpty( outputDirectory ) )
            {
                Console.MarkupLine( "[red]Plugin directory is not empty and might overwrite existing files.[/]" );
                Console.MarkupLineInterpolated( $"[red]  Directory: {outputDirectory}[/]" );
                Console.WriteLine();
                Console.WriteLine( "To create the plugin anyway, run the command with '--force' option." );
                Console.WriteLine();

                return 1;
            }
        }

        var result = await GeneratePluginAsync( outputDirectory );

        if ( !result.Successful )
        {
            if ( result.Message != null )
            {
                Console.WriteLine( result.Message );
            }

            return 1;
        }

        if ( !Repository.IsValid( outputDirectory ) )
        {
            if ( ExecuteOptions.DryRun )
            {
                var friendlyPath = _fs.Path.GetFriendlyPath( outputDirectory );

                Console.MarkupLineInterpolated( $"Create git repository [cyan]{friendlyPath}[/]." );
            }
            else
            {
                Repository.Init( outputDirectory );
                using var repo = new Repository( outputDirectory );

                // Switch the repository to use "main" as the default branch.
                repo.Refs.Add( "HEAD", "refs/heads/main", null, true );
            }
        }

        var pluginRelativePath = _fs.Path.GetRelativePath( environmentDirectory, outputDirectory )
            .Replace( '\\', '/' );

        if ( _environment.GetPlugins().Any( p => p.Path == pluginRelativePath ) )
        {
            _environment.AddPlugin( pluginRelativePath );
        }

        _environment.Save();

        AddPluginToGitIgnore();
        AddPluginToSolutionFile();

        return 0;
    }

    /// <summary>
    /// Generate all the project files needed for the specified options.
    /// </summary>
    /// <param name="outputDirectory">The directory to output all the new content into.
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> GeneratePluginAsync( string outputDirectory )
    {
        var sdkProjectDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}";
        var obsidianProjectDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian";

        await CopyTemplateAsync( "plugin.plugin.json.template", [outputDirectory, "plugin.json"] );

        sdkProjectDirectory = _fs.Path.Combine( outputDirectory, sdkProjectDirectory );
        obsidianProjectDirectory = _fs.Path.Combine( outputDirectory, obsidianProjectDirectory );

        if ( ExecuteOptions.DllProject == true )
        {
            var result = await CreateSdkProject( sdkProjectDirectory );

            if ( !result.Successful )
            {
                return result;
            }
        }

        if ( ExecuteOptions.ObsidianProject == true )
        {
            var result = await CreateObsidianProject( obsidianProjectDirectory );

            if ( !result.Successful )
            {
                return result;
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Generate all the project files needed for the C# project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> CreateSdkProject( string directory )
    {
        var projectFilename = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.csproj";

        CreateDirectory( directory );

        await CopyTemplateAsync( "sdk_project.project.csproj.template", [directory, projectFilename] );
        await CopyTemplateAsync( "sdk_project.Class1.cs.template", [directory, "Class1.cs"] );
        await CopyTemplateAsync( "sdk_project.gitignore.template", [directory, ".gitignore"] );

        return ValidationResult.Success();
    }

    /// <summary>
    /// Generate all the project files needed for the Obsidian project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> CreateObsidianProject( string directory )
    {
        var projectFilename = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian.esproj";

        CreateDirectory( directory );

        await CopyTemplateAsync( "obsidian_project.eslintrc.json.template", [directory, ".eslintrc.json"] );
        await CopyTemplateAsync( "obsidian_project.gitignore.template", [directory, ".gitignore"] );
        await CopyTemplateAsync( "obsidian_project.package.json.template", [directory, "package.json"] );
        await CopyTemplateAsync( "obsidian_project.obsidian.config.json.template", [directory, "obsidian.config.json"] );
        await CopyTemplateAsync( "obsidian_project.project.esproj.template", [directory, projectFilename] );
        await CopyTemplateAsync( "obsidian_project.tsconfig.base.json.template", [directory, "tsconfig.base.json"] );

        await CopyTemplateAsync( "obsidian_project.src.shims-obs.d.ts.template", [directory, "src", "shims-obs.d.ts"] );
        await CopyTemplateAsync( "obsidian_project.src.tsconfig.json.template", [directory, "src", "tsconfig.json"] );

        await CopyTemplateAsync( "obsidian_project.tests.sample.spec.ts.template", [directory, "tests", "sample.spec.ts"] );
        await CopyTemplateAsync( "obsidian_project.tests.tsconfig.json.template", [directory, "tests", "tsconfig.json"] );

        return ValidationResult.Success();
    }

    /// <summary>
    /// Copies a template into the destination file after resolving any
    /// merge fields.
    /// </summary>
    /// <param name="sourceName">The source template name.</param>
    /// <param name="destination">The array of destination path components.</param>
    private async Task CopyTemplateAsync( string sourceName, string[] destination )
    {
        using var stream = GetType().Assembly.GetManifestResourceStream( $"SparkDevNetwork.Rock.DevTool.Templates.{sourceName}" )
            ?? throw new ArgumentOutOfRangeException( nameof( sourceName ), $"Template {sourceName} not found" );
        using var textStream = new StreamReader( stream );
        var rawContent = await textStream.ReadToEndAsync();
        var parser = new FluidParser();

        if ( !parser.TryParse( rawContent, out var template, out var error ) )
        {
            throw new Exception( error );
        }

        var options = new NewCommandOptions( ExecuteOptions );

        if ( options.RockWebPath is not null )
        {
            var rockWebPrefix = new List<string>
            {
                ".."
            };

            // The RockWeb path is relative to the current directory. So we need
            // to adjust it to be relative to the sub-directory.
            for ( int i = 0; i < destination.Length - 1; i++ )
            {
                rockWebPrefix.Add( ".." );
            }

            var userPath = options.RockWebPath
                .Replace( '/', _fs.Path.DirectorySeparatorChar )
                .Replace( '\\', _fs.Path.DirectorySeparatorChar );
            var fullPath = _fs.Path.GetFullPath( _fs.Path.Combine( [.. rockWebPrefix, userPath] ) );

            options.RockWebPath = _fs.Path.GetRelativePath( _fs.Directory.GetCurrentDirectory(), fullPath );
        }

        var content = await template.RenderAsync( new TemplateContext( options ) );
        var destinationDirectory = _fs.Path.GetDirectoryName( _fs.Path.Combine( destination ) );

        if ( destinationDirectory != null )
        {
            CreateDirectory( destinationDirectory );
        }

        WriteFile( _fs.Path.Combine( destination ), content );
    }

    /// <summary>
    /// Open the environment or return null if the environment is not valid.
    /// </summary>
    /// <returns>An instance of <see cref="DevEnvironment.Environment"/> or <c>null</c>.</returns>
    private DevEnvironment.Environment? OpenEnvironment()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        environmentDirectory = _fs.Path.GetFullPath( environmentDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );
            environment.IsDryRun = ExecuteOptions.DryRun;

            return environment;
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return null;
        }
    }

    /// <summary>
    /// Adds the newly created plugin to the .gitignore file.
    /// </summary>
    private void AddPluginToGitIgnore()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var path = _fs.Path.Combine( environmentDirectory, ".gitignore" );

        var content = _fs.File.ReadAllText( path );
        var lineEnding = content.Contains( "\r\n" ) ? "\r\n" : "\n";

        if ( !content.EndsWith( lineEnding ) )
        {
            content = $"{content}{lineEnding}/{ExecuteOptions.PluginCode}{lineEnding}";
        }
        else
        {
            content = $"{content}/{ExecuteOptions.PluginCode}{lineEnding}";
        }

        WriteFile( path, content );
    }

    /// <summary>
    /// Adds the new plugin projects to the solution file.
    /// </summary>
    private void AddPluginToSolutionFile()
    {
        var environmentDirectory = ExecuteOptions.EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var path = _fs.Path.Combine( environmentDirectory, $"{_environment.GetOrganizationName()?.Replace( " ", "" )}.sln" );

        if ( !_fs.File.Exists( path ) )
        {
            return;
        }

        var content = _fs.File.ReadAllText( path );
        var lineEnding = content.Contains( "\r\n" ) ? "\r\n" : "\n";
        var csprojGuid = Guid.NewGuid().ToString().ToUpper();
        var tsprojGuid = Guid.NewGuid().ToString().ToUpper();
        string? csprojName = null;
        string? csprojPath = null;
        string? tsprojName = null;
        string? tsprojPath = null;

        if ( ExecuteOptions.DllProject == true )
        {
            csprojName = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}";
            csprojPath = $"{ExecuteOptions.PluginCode}\\{csprojName}\\{csprojName}.csproj";
        }

        if ( ExecuteOptions.ObsidianProject == true )
        {
            tsprojName = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian";
            tsprojPath = $"{ExecuteOptions.PluginCode}\\{tsprojName}\\{tsprojName}.esproj";
        }

        content = GlobalSectionRegex().Replace( content, m =>
        {
            var text = string.Empty;

            if ( csprojName != null && csprojPath != null )
            {
                text += $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{csprojName}\", \"{csprojPath}\", \"{{{csprojGuid}}}\"{lineEnding}";
                text += $"EndProject{lineEnding}";
            }

            if ( tsprojName != null && tsprojPath != null )
            {
                text += $"Project(\"{{54A90642-561A-4BB1-A94E-469ADEE60C69}}\") = \"{tsprojName}\", \"{tsprojPath}\", \"{{{tsprojGuid}}}\"{lineEnding}";
                text += $"EndProject{lineEnding}";
            }

            return text + m.Value;
        } );

        content = ProjectConfigurationRegex().Replace( content, m =>
        {
            var text = string.Empty;

            if ( csprojName != null )
            {
                text += $"\t\t{{{csprojGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU{lineEnding}";
                text += $"\t\t{{{csprojGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU{lineEnding}";
                text += $"\t\t{{{csprojGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU{lineEnding}";
                text += $"\t\t{{{csprojGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU{lineEnding}";
            }

            if ( tsprojName != null )
            {
                text += $"\t\t{{{tsprojGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU{lineEnding}";
                text += $"\t\t{{{tsprojGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU{lineEnding}";
                text += $"\t\t{{{tsprojGuid}}}.Debug|Any CPU.Deploy.0 = Debug|Any CPU{lineEnding}";
                text += $"\t\t{{{tsprojGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU{lineEnding}";
                text += $"\t\t{{{tsprojGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU{lineEnding}";
                text += $"\t\t{{{tsprojGuid}}}.Release|Any CPU.Deploy.0 = Release|Any CPU{lineEnding}";
            }

            return m.Groups[1].Value + text + m.Groups[2].Value;
        } );

        WriteFile( path, content );
    }

    /// <summary>
    /// Update the options with values from the development environment.
    /// </summary>
    private void PopulateOptionsFromEnvironment()
    {
        if ( ExecuteOptions.RockVersion == null )
        {
            ExecuteOptions.RockVersion = _environment.GetRockVersion();
        }

        if ( ExecuteOptions.Organization == null )
        {
            ExecuteOptions.Organization = _environment.GetOrganizationName();
        }

        if ( ExecuteOptions.OrganizationCode == null )
        {
            ExecuteOptions.OrganizationCode = _environment.GetOrganizationCode();
        }

        if ( ExecuteOptions.RockWebPath == null && _environment.GetRockVersion() != null )
        {
            ExecuteOptions.RockWebPath = "Rock/RockWeb";
        }
    }

    /// <summary>
    /// Prompts for any missing options and updates the options object.
    /// </summary>
    private void PromptForMissingOptions()
    {
        ExecuteOptions.Organization = new TextPrompt<string?>( "Organization?" )
            .DefaultValue( ExecuteOptions.Organization )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.OrganizationCode = new TextPrompt<string?>( "Organization Code?" )
            .DefaultValue( ExecuteOptions.OrganizationCode )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.PluginName = new TextPrompt<string?>( "Plugin Name?" )
            .Show( Console );

        var rockVersionString = new TextPrompt<string?>( "Rock Version?" )
            .DefaultValue( ExecuteOptions.RockVersion?.ToString() )
            .DefaultValueStyle( "blue" )
            .Validate( VersionStringValidator )
            .Show( Console );

        ExecuteOptions.RockVersion = SemVersion.Parse( rockVersionString, SemVersionStyles.Strict );

        ExecuteOptions.RockWebPath = new TextPrompt<string?>( "Path to RockWeb?" )
            .DefaultValue( ExecuteOptions.RockWebPath )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.DllProject = new ConfirmationPrompt( "Create DLL Project?" )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.ObsidianProject = new ConfirmationPrompt( "Create Obsidian Project?" )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.Copy = new ConfirmationPrompt( "Copy artifacts to RockWeb?" )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        Console.WriteLine();
    }

    /// <summary>
    /// Validate the string to make sure it is a valid version number.
    /// </summary>
    /// <param name="value">The text entered on the prompt.</param>
    /// <returns>The result of the validation.</returns>
    private ValidationResult VersionStringValidator( string? value )
    {
        if ( string.IsNullOrWhiteSpace( value ) )
        {
            return ValidationResult.Error( "A value is required." );
        }
        if ( !SemVersion.TryParse( value, SemVersionStyles.Strict, out _ ) )
        {
            return ValidationResult.Error( "That is not a valid version number." );
        }
        else
        {
            return ValidationResult.Success();
        }
    }

    [GeneratedRegex( "^Global", RegexOptions.Multiline )]
    private static partial Regex GlobalSectionRegex();

    [GeneratedRegex( "(GlobalSection\\(ProjectConfigurationPlatforms\\).*?)([\\t ]+EndGlobalSection)", RegexOptions.Singleline )]
    private static partial Regex ProjectConfigurationRegex();
}
