using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;

using Fluid;

using LibGit2Sharp;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using SparkDevNetwork.Rock.DevTool.DevEnvironment;
using SparkDevNetwork.Rock.DevTool.DevEnvironment.Sln;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.PluginCommands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
partial class NewCommand : Abstractions.BaseModifyCommand
{
    #region Fields

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

    /// <summary>
    /// The provider of services for this instance.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The option that defines the directory of the environment.
    /// </summary>
    private readonly Option<string?> _environmentOption;

    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// The environment for this command. This will be valid once execution
    /// of the command has started.
    /// </summary>
    private DevEnvironment.Environment _environment = null!;

    #endregion

    #region Properties

    /// <summary>
    /// The path to create the new environment in.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// The directory that contains the environment.
    /// </summary>
    public string? EnvironmentPath { get; set; }

    /// <summary>
    /// The name of the organization such as <c>Rock Solid Church</c>.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// The namespace code of the organization such as <c>com.rocksolidchurch</c>.
    /// </summary>
    public string? OrganizationCode { get; set; }

    /// <summary>
    /// The name of the plugin such as <c>Sample Data</c>.
    /// </summary>
    public string? PluginName { get; set; }

    /// <summary>
    /// The namespace code to use for the plugin name such as <c>SampleData</c>.
    /// </summary>
    public string? PluginCode => PluginName?.Replace( " ", "" );

    /// <summary>
    /// The version number of Rock the plugin will target such as <c>1.16.0</c>.
    /// </summary>
    public SemVersion? RockVersion { get; set; }

    /// <summary>
    /// The relative path to the RockWeb folder such as <c>../RockWeb</c>.
    /// </summary>
    public string? RockWebPath { get; set; }

    /// <summary>
    /// Determines if the C# project will be created.
    /// </summary>
    public bool? DllProject { get; set; }

    /// <summary>
    /// Determines if support for creating REST APIs will be included in the
    /// DLL project.
    /// </summary>
    public bool? RestApiSupport { get; set; }

    /// <summary>
    /// Determines if the Obsidian project will be created.
    /// </summary>
    public bool? ObsidianProject { get; set; }

    /// <summary>
    /// Determines if the legacy WebForms folder will be created.
    /// </summary>
    public bool? LegacyWebForms { get; set; }

    /// <summary>
    /// Determines if the build artifacts will be copied to <see cref="RockWebPath" />.
    /// </summary>
    public bool? Copy { get; set; }

    #endregion

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
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        EnvironmentPath = context.ParseResult.GetValueForOption( _environmentOption );
        Output = context.ParseResult.GetValueForOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var env = OpenEnvironment();

        if ( env == null )
        {
            return 1;
        }

        _environment = env;

        PopulateOptionsFromEnvironment();

        PromptForMissingOptions();

        var outputDirectory = Output ?? _fs.Path.Combine( environmentDirectory, PluginCode! );

        if ( !Force )
        {
            if ( env.GetPlugins().Any( p => p.Path == PluginCode ) )
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
            if ( DryRun )
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

        if ( !_environment.GetPlugins().Any( p => p.Path == pluginRelativePath ) )
        {
            _environment.AddPlugin( pluginRelativePath );
        }

        _environment.Save();

        AddPluginToGitIgnore();
        AddPluginToSolutionFile();

        var environmentPlugin = _environment.GetPlugins().First( p => p.Path == pluginRelativePath );
        _environment.SetupPlugin( environmentPlugin );

        return 0;
    }

    /// <summary>
    /// Generate all the project files needed for the specified options.
    /// </summary>
    /// <param name="outputDirectory">The directory to output all the new content into.
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> GeneratePluginAsync( string outputDirectory )
    {
        var sdkProjectDirectory = $"{OrganizationCode}.{PluginCode}";
        var obsidianProjectDirectory = $"{OrganizationCode}.{PluginCode}.Obsidian";
        var webFormsDirectory = "WebForms";

        await CopyTemplateAsync( "plugin.plugin.json.template", [outputDirectory, "plugin.json"] );

        sdkProjectDirectory = _fs.Path.Combine( outputDirectory, sdkProjectDirectory );
        obsidianProjectDirectory = _fs.Path.Combine( outputDirectory, obsidianProjectDirectory );
        webFormsDirectory = _fs.Path.Combine( outputDirectory, webFormsDirectory );

        if ( DllProject == true )
        {
            var result = await CreateSdkProject( sdkProjectDirectory );

            if ( !result.Successful )
            {
                return result;
            }
        }

        if ( ObsidianProject == true )
        {
            var result = await CreateObsidianProject( obsidianProjectDirectory );

            if ( !result.Successful )
            {
                return result;
            }
        }

        if ( LegacyWebForms == true )
        {
            var webFormsIgnore = $"*.*{System.Environment.NewLine}!*.ascx{System.Environment.NewLine}!*.ascx.cs{System.Environment.NewLine}!.gitignore{System.Environment.NewLine}";

            CreateDirectory( webFormsDirectory );
            WriteFile( _fs.Path.Combine( webFormsDirectory, ".gitignore" ), webFormsIgnore );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Generate all the project files needed for the C# project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> CreateSdkProject( string directory )
    {
        var projectFilename = $"{OrganizationCode}.{PluginCode}.csproj";

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
        var projectFilename = $"{OrganizationCode}.{PluginCode}.Obsidian.esproj";

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

        var options = new TemplateMergeFields
        {
            Organization = Organization,
            OrganizationCode = OrganizationCode,
            PluginName = PluginName,
            PluginCode = PluginCode,
            RockVersion = RockVersion,
            RockWebPath = RockWebPath,
            Copy = Copy ?? false,
            RestApiSupport = RestApiSupport ?? false,
            ToolVersion = GetToolVersion()
        };

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
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        environmentDirectory = _fs.Path.GetFullPath( environmentDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );
            environment.IsDryRun = DryRun;

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
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var path = _fs.Path.Combine( environmentDirectory, ".gitignore" );

        var content = _fs.File.ReadAllText( path );
        var lineEnding = content.Contains( "\r\n" ) ? "\r\n" : "\n";

        // If the gitignore file already contains this item then skip it. This
        // most often happens when the force option is used.
        if ( content.Split( ['\r', '\n'] ).Contains( $"/{PluginCode}" ) )
        {
            return;
        }

        if ( !content.EndsWith( lineEnding ) )
        {
            content = $"{content}{lineEnding}/{PluginCode}{lineEnding}";
        }
        else
        {
            content = $"{content}/{PluginCode}{lineEnding}";
        }

        WriteFile( path, content );
    }

    /// <summary>
    /// Adds the new plugin projects to the solution file.
    /// </summary>
    private void AddPluginToSolutionFile()
    {
        var environmentDirectory = EnvironmentPath ?? _fs.Directory.GetCurrentDirectory();
        var path = _fs.Path.Combine( environmentDirectory, $"{_environment.GetOrganizationName()?.Replace( " ", "" )}.sln" );

        if ( !_fs.File.Exists( path ) )
        {
            return;
        }

        var content = _fs.File.ReadAllText( path );
        var sln = new SlnReader().ParseString( content );
        var rockWebProject = sln.Projects.FirstOrDefault( p => p.Name == "RockWeb" );

        if ( DllProject == true )
        {
            var csprojName = $"{OrganizationCode}.{PluginCode}";
            var csprojPath = $"{PluginCode}\\{csprojName}\\{csprojName}.csproj";

            var project = sln.AddProject( csprojName, csprojPath, Project.ProjectTypeCSharp );

            rockWebProject?.AddProjectDependency( project.ProjectGuid );
        }

        if ( ObsidianProject == true )
        {
            var tsprojName = $"{OrganizationCode}.{PluginCode}.Obsidian";
            var tsprojPath = $"{PluginCode}\\{tsprojName}\\{tsprojName}.esproj";

            var project = sln.AddProject( tsprojName, tsprojPath, Project.ProjectTypeJavaScript );

            rockWebProject?.AddProjectDependency( project.ProjectGuid );
        }

        content = new SlnWriter().WriteToString( sln );

        WriteFile( path, content );
    }

    /// <summary>
    /// Update the options with values from the development environment.
    /// </summary>
    private void PopulateOptionsFromEnvironment()
    {
        if ( RockVersion == null )
        {
            RockVersion = _environment.GetRockVersion();
        }

        if ( Organization == null )
        {
            Organization = _environment.GetOrganizationName();
        }

        if ( OrganizationCode == null )
        {
            OrganizationCode = _environment.GetOrganizationCode();
        }

        if ( RockWebPath == null && _environment.GetRockVersion() != null )
        {
            RockWebPath = "Rock/RockWeb";
        }
    }

    /// <summary>
    /// Prompts for any missing options and updates the options object.
    /// </summary>
    private void PromptForMissingOptions()
    {
        Organization = new TextPrompt<string?>( "Organization?" )
            .DefaultValue( Organization )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        OrganizationCode = new TextPrompt<string?>( "Organization Code?" )
            .DefaultValue( OrganizationCode )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        PluginName = new TextPrompt<string?>( "Plugin Name?" )
            .Show( Console );

        var rockVersionString = new TextPrompt<string?>( "Rock Version?" )
            .DefaultValue( RockVersion?.ToString() )
            .DefaultValueStyle( "blue" )
            .Validate( VersionStringValidator )
            .Show( Console );

        RockVersion = SemVersion.Parse( rockVersionString, SemVersionStyles.Strict );

        RockWebPath = new TextPrompt<string?>( "Path to RockWeb?" )
            .DefaultValue( RockWebPath )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        DllProject = new ConfirmationPrompt( "Create DLL Project?" )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        if ( DllProject == true )
        {
            RestApiSupport = new ConfirmationPrompt( "- Add support for REST endpoints?" )
            {
                DefaultValue = false
            }
                .DefaultValueStyle( "blue" )
                .Show( Console );
        }

        ObsidianProject = new ConfirmationPrompt( "Create Obsidian Project?" )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        LegacyWebForms = new ConfirmationPrompt( "Create Legacy WebForms Directory?" )
        {
            DefaultValue = false
        }
            .DefaultValueStyle( "blue" )
            .Show( Console );

        Copy = new ConfirmationPrompt( "Copy artifacts to RockWeb?" )
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

    /// <summary>
    /// Gets the tool version number. This is taken from the build version
    /// but excludes any build metadata (git commit).
    /// </summary>
    /// <returns>A string that represents the build number.</returns>
    private string GetToolVersion()
    {
        var currentVersionInfo = GetType()
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0";

        return currentVersionInfo.Split( '+' )[0];
    }

    /// <summary>
    /// The merge fields that will be available when merging template source
    /// files.
    /// </summary>
    private class TemplateMergeFields
    {
        /// <summary>
        /// The name of the organization such as <c>Rock Solid Church</c>.
        /// </summary>
        public string? Organization { get; set; }

        /// <summary>
        /// The namespace code of the organization such as <c>com.rocksolidchurch</c>.
        /// </summary>
        public string? OrganizationCode { get; set; }

        /// <summary>
        /// The name of the plugin such as <c>Sample Data</c>.
        /// </summary>
        public string? PluginName { get; set; }

        /// <summary>
        /// The namespace code to use for the plugin name such as <c>SampleData</c>.
        /// </summary>
        public string? PluginCode { get; set; }

        /// <summary>
        /// The version number of Rock the plugin will target such as <c>1.16.0</c>.
        /// </summary>
        public Semver.SemVersion? RockVersion { get; set; }

        /// <summary>
        /// The relative path to the RockWeb folder such as <c>../RockWeb</c>.
        /// </summary>
        public string? RockWebPath { get; set; }

        /// <summary>
        /// Determines if the build artifacts will be copied to <see cref="RockWebPath" />.
        /// </summary>
        public bool? Copy { get; set; }

        /// <summary>
        /// Determines if support for creating REST APIs will be included in the
        /// DLL project.
        /// </summary>
        public bool? RestApiSupport { get; set; }

        /// <summary>
        /// The version number of the tool when the merge happens.
        /// </summary>
        public string? ToolVersion { get; set; }
    }
}
