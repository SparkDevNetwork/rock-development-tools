using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;

using Fluid;

using Microsoft.Extensions.DependencyInjection;

using Semver;

using Sharprompt;

using Spectre.Console;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.PluginCommands;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class CreateCommand : Abstractions.BaseModifyCommand<CreateCommandOptions>
{
    private readonly IFileSystem _fs;

    private readonly Option<string?> _organizationOption;

    private readonly Option<string?> _organizationCodeOption;

    private readonly Option<string?> _nameOption;

    private readonly Option<string?> _rockVersionOption;

    private readonly Option<string?> _rockWebOption;

    private readonly Option<bool?> _obsidianOption;

    private readonly Option<bool?> _copyOption;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public CreateCommand( IServiceProvider serviceProvider )
        : base( "create", "Creates a new plugin following a standard template.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _organizationOption = new Option<string?>( "--organization", "The name of the organization that owns the plugin" );
        _organizationCodeOption = new Option<string?>( "--organization-code", "The namespace-style code used for the organization" );
        _nameOption = new Option<string?>( "--name", "The name of the plugin" );
        _rockVersionOption = new Option<string?>( "--rock-version", "The version of Rock to base the plugin on" )
            .FromAmong( Support.SupportedRockVersions );
        _rockWebOption = new Option<string?>( "--rock-web", "The path to the RockWeb folder" );
        _obsidianOption = new Option<bool?>( "--obsidian", "Determines if an Obsidian project is created" );
        _copyOption = new Option<bool?>( "--copy", "Copies the built artifacts to the RockWeb folder automatically" );

        AddOption( _organizationOption );
        AddOption( _organizationCodeOption );
        AddOption( _nameOption );
        AddOption( _rockVersionOption );
        AddOption( _rockWebOption );
        AddOption( _obsidianOption );
        AddOption( _copyOption );
    }

    /// <inheritdoc/>
    protected override CreateCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Organization = context.ParseResult.GetValueForOption( _organizationOption );
        options.OrganizationCode = context.ParseResult.GetValueForOption( _organizationCodeOption );
        options.PluginName = context.ParseResult.GetValueForOption( _nameOption );
        options.RockWebPath = context.ParseResult.GetValueForOption( _rockWebOption );
        options.Obsidian = context.ParseResult.GetValueForOption( _obsidianOption );
        options.Copy = context.ParseResult.GetValueForOption( _copyOption );

        if ( SemVersion.TryParse( context.ParseResult.GetValueForOption( _rockVersionOption ), SemVersionStyles.Strict, out var version ) )
        {
            options.RockVersion = version;
        }

        return options;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync()
    {
        PromptForMissingOptions();

        var result = await GenerateProjectsAsync();

        if ( result is not null )
        {
            if ( result.ErrorMessage != null )
            {
                Console.WriteLine( result.ErrorMessage );
            }

            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Generate all the project files needed for the specified options.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult?> GenerateProjectsAsync()
    {
        var csharpDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}";
        var obsidianDirectory = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.Obsidian";

        if ( _fs.Path.Exists( csharpDirectory ) )
        {
            return new ValidationResult( $"Directory {csharpDirectory} already exists, aborting." );
        }

        if ( ExecuteOptions.Obsidian == true && _fs.Path.Exists( obsidianDirectory ) )
        {
            return new ValidationResult( $"Directory {obsidianDirectory} already exists, aborting." );
        }

        return await CreateCSharpProject( csharpDirectory )
            ?? await CreateObsidianProject( obsidianDirectory );
    }

    /// <summary>
    /// Generate all the project files needed for the C# project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult?> CreateCSharpProject( string directory )
    {
        var projectFilename = $"{ExecuteOptions.OrganizationCode}.{ExecuteOptions.PluginCode}.csproj";

        _fs.Directory.CreateDirectory( directory );

        await CopyTemplateAsync( "CSharp.project.csproj", [directory, projectFilename] );
        await CopyTemplateAsync( "CSharp.Class1.cs", [directory, "Class1.cs"] );
        await CopyTemplateAsync( "CSharp.gitignore", [directory, ".gitignore"] );

        return null;
    }

    /// <summary>
    /// Generate all the project files needed for the Obsidian project.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult?> CreateObsidianProject( string directory )
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

        return null;
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

    /// <summary>
    /// Prompts for any missing options and updates the options object.
    /// </summary>
    private void PromptForMissingOptions()
    {
        if ( string.IsNullOrEmpty( ExecuteOptions.Organization ) )
        {
            ExecuteOptions.Organization = Prompt.Input<string>( "Organization",
                placeholder: "Rock Solid Church Demo",
                validators: [Validators.Required()] );
        }

        if ( string.IsNullOrEmpty( ExecuteOptions.OrganizationCode ) )
        {
            ExecuteOptions.OrganizationCode = Prompt.Input<string>( "Organization Code",
                placeholder: $"com.rocksolidchurchdemo",
                validators: [Validators.Required()] );
        }

        if ( string.IsNullOrEmpty( ExecuteOptions.PluginName ) )
        {
            ExecuteOptions.PluginName = Prompt.Input<string>( "Plugin Name",
                validators: [Validators.Required()] );
        }

        if ( ExecuteOptions.RockVersion is null )
        {
            var versionText = Prompt.Select( "Rock Version", Support.SupportedRockVersions );

            ExecuteOptions.RockVersion = SemVersion.Parse( versionText, SemVersionStyles.Strict );
        }

        if ( ExecuteOptions.RockWebPath is null )
        {
            var possibleRockWebPaths = new string[] {
                "RockWeb",
                _fs.Path.Combine( "Rock", "RockWeb" )
            };
            var defaultRockWebPath = "";

            foreach ( var p in possibleRockWebPaths )
            {
                if ( _fs.Path.Exists( _fs.Path.Combine( p, "web.config" ) ) )
                {
                    defaultRockWebPath = p;
                    break;
                }
            }

            ExecuteOptions.RockWebPath = Prompt.Input<string>( "Path to RockWeb",
                validators: [ValidateRockWebPathPrompt] );
        }

        ExecuteOptions.Obsidian ??= Prompt.Confirm( "Create Obsidian Project" );

        if ( ExecuteOptions.Copy == null )
        {
            if ( ExecuteOptions.RockWebPath != null )
            {
                ExecuteOptions.Copy = Prompt.Confirm( "Copy artifacts to RockWeb" );
            }
            else
            {
                ExecuteOptions.Copy = false;
            }
        }
    }

    /// <summary>
    /// Validates the Rock version specified from the UI prompt against
    /// versions that are supported.
    /// </summary>
    /// <param name="value">The value entered in the prompt.</param>
    /// <returns>The validation error or <c>null</c> if it was valid.</returns>
    private ValidationResult? ValidateRockWebPathPrompt( object value )
    {
        if ( value is string stringValue )
        {
            if ( !_fs.Path.Exists( stringValue ) )
            {
                return new ValidationResult( "That path does not appear to exist" );
            }

            if ( !_fs.Path.Exists( _fs.Path.Combine( stringValue, "web.config" ) ) )
            {
                return new ValidationResult( "That path does not appear to be a RockWeb path" );
            }
        }

        return ValidationResult.Success;
    }
}
