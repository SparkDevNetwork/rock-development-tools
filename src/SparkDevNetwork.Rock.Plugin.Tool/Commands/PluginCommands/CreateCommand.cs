using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;

using Fluid;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using SparkDevNetwork.Rock.Plugin.Tool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.PluginCommands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class CreateCommand : Abstractions.BaseModifyCommand<CreateCommandOptions>
{
    private readonly IFileSystem _fs;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The option that defines the target directory of the environment.
    /// </summary>
    private readonly Option<string?> _targetOption;

    /// <summary>
    /// The option that defines the output directory of the new environment.
    /// </summary>
    private readonly Option<string?> _outputOption;

    private DevEnvironment.Environment _environment = null!;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public CreateCommand( IServiceProvider serviceProvider )
        : base( "create", "Creates a new plugin following a standard template.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();
        _serviceProvider = serviceProvider;

        _targetOption = new Option<string?>( "--target", "The directory that contains the environment." );
        _targetOption.AddAlias( "-t" );

        _outputOption = new Option<string?>( "--output", "Location to place the generated output in the environment." );
        _outputOption.AddAlias( "-o" );

        AddOption( _targetOption );
        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override CreateCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Target = context.ParseResult.GetValueForOption( _targetOption );
        options.Output = context.ParseResult.GetValueForOption( _outputOption );

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        var env = OpenEnvironment();

        if ( env == null )
        {
            return 1;
        }

        _environment = env;

        PopulateOptionsFromEnvironment();

        PromptForMissingOptions();

        var result = await GenerateProjectsAsync();

        if ( result is not null )
        {
            if ( result.Message != null )
            {
                Console.WriteLine( result.Message );
            }

            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Generate all the project files needed for the specified options.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> GenerateProjectsAsync()
    {
        System.Diagnostics.Debugger.Launch();
        var environmentDirectory = ExecuteOptions.Target ?? _fs.Directory.GetCurrentDirectory();
        var csharpDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}";
        var obsidianDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian";

        csharpDirectory = _fs.Path.Combine( environmentDirectory, csharpDirectory );
        obsidianDirectory = _fs.Path.Combine( environmentDirectory, obsidianDirectory );

        if ( ExecuteOptions.DllProject == true && _fs.Path.Exists( csharpDirectory ) )
        {
            return ValidationResult.Error( $"Directory {csharpDirectory} already exists, aborting." );
        }

        if ( ExecuteOptions.ObsidianProject == true && _fs.Path.Exists( obsidianDirectory ) )
        {
            return ValidationResult.Error( $"Directory {obsidianDirectory} already exists, aborting." );
        }

        if ( ExecuteOptions.DllProject == true )
        {
            var result = await CreateCSharpProject( csharpDirectory );

            if ( !result.Successful )
            {
                return result;
            }
        }

        if ( ExecuteOptions.ObsidianProject == true )
        {
            var result = await CreateObsidianProject( obsidianDirectory );

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
    private async Task<ValidationResult> CreateCSharpProject( string directory )
    {
        var projectFilename = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.csproj";

        _fs.Directory.CreateDirectory( directory );

        await CopyTemplateAsync( "CSharp.project.csproj", [directory, projectFilename] );
        await CopyTemplateAsync( "CSharp.Class1.cs", [directory, "Class1.cs"] );
        await CopyTemplateAsync( "CSharp.gitignore", [directory, ".gitignore"] );

        return ValidationResult.Success();
    }

    /// <summary>
    /// Generate all the project files needed for the Obsidian project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult> CreateObsidianProject( string directory )
    {
        var projectFilename = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian.esproj";

        _fs.Directory.CreateDirectory( directory );

        await CopyTemplateAsync( "Obsidian.eslintrc.json", [directory, ".eslintrc.json"] );
        await CopyTemplateAsync( "Obsidian.gitignore", [directory, ".gitignore"] );
        await CopyTemplateAsync( "Obsidian.package.json", [directory, "package.json"] );
        await CopyTemplateAsync( "Obsidian.obsidian.config.json", [directory, "obsidian.config.json"] );
        await CopyTemplateAsync( "Obsidian.project.esproj", [directory, projectFilename] );
        await CopyTemplateAsync( "Obsidian.tsconfig.base.json", [directory, "tsconfig.base.json"] );

        await CopyTemplateAsync( "Obsidian.src.shims-obs.d.ts", [directory, "src", "shims-obs.d.ts"] );
        await CopyTemplateAsync( "Obsidian.src.tsconfig.json", [directory, "src", "tsconfig.json"] );

        await CopyTemplateAsync( "Obsidian.tests.sample.spec.ts", [directory, "tests", "sample.spec.ts"] );
        await CopyTemplateAsync( "Obsidian.tests.tsconfig.json", [directory, "tests", "tsconfig.json"] );

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
        using var stream = GetType().Assembly.GetManifestResourceStream( $"SparkDevNetwork.Rock.Plugin.Tool.Templates.{sourceName}" )
            ?? throw new ArgumentOutOfRangeException( nameof( sourceName ), $"Template {sourceName} not found" );
        using var textStream = new StreamReader( stream );
        var rawContent = await textStream.ReadToEndAsync();
        var parser = new FluidParser();

        if ( !parser.TryParse( rawContent, out var template, out var error ) )
        {
            throw new Exception( error );
        }

        var options = new CreateCommandOptions( ExecuteOptions );

        if ( options.RockWebPath is not null )
        {
            var rockWebPrefix = new List<string>();

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
            _fs.Directory.CreateDirectory( destinationDirectory );
        }

        await _fs.File.WriteAllTextAsync( _fs.Path.Combine( destination ), content );
    }

    private DevEnvironment.Environment? OpenEnvironment()
    {
        var environmentDirectory = ExecuteOptions.Target ?? _fs.Directory.GetCurrentDirectory();
        DevEnvironment.Environment environment;

        environmentDirectory = _fs.Path.GetFullPath( environmentDirectory );

        try
        {
            environment = DevEnvironment.Environment.Open( environmentDirectory, _serviceProvider );

            return environment;
        }
        catch ( InvalidEnvironmentException ex )
        {
            Console.WriteLine( ex.Message );
            return null;
        }
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
        ExecuteOptions.Organization = new TextPrompt<string?>( "Organization" )
            .DefaultValue( ExecuteOptions.Organization )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.OrganizationCode = new TextPrompt<string?>( "Organization Code" )
            .DefaultValue( ExecuteOptions.OrganizationCode )
            .DefaultValueStyle( "blue" )
            .Show( Console );

        ExecuteOptions.PluginName = new TextPrompt<string?>( "Plugin Name" )
            .Show( Console );

        var rockVersionString = new TextPrompt<string?>( "Rock Version" )
            .DefaultValue( ExecuteOptions.RockVersion?.ToString() )
            .DefaultValueStyle( "blue" )
            .Validate( VersionStringValidator )
            .Show( Console );

        ExecuteOptions.RockVersion = SemVersion.Parse( rockVersionString, SemVersionStyles.Strict );

        ExecuteOptions.RockWebPath = new TextPrompt<string?>( "Path to RockWeb" )
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
}
