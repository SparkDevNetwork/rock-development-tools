using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;

using Fluid;

using Semver;

using Sharprompt;

using SparkDevNetwork.Rock.Plugin.Tool.CommandOptions.Plugin;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.Plugin;

/// <summary>
/// Container for sub-commands related to working with plugins.
/// </summary>
class CreateCommand : ModifyCommandBase<CreateCommandOptions>
{
    /// <summary>
    /// The options that have been resolved for this command.
    /// </summary>
    private CreateCommandOptions _options = new();

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
    public CreateCommand()
        : base( "create", "Creates a new plugin following a standard template.." )
    {
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
    protected override async Task<int> ExecuteAsync( CreateCommandOptions options )
    {
        _options = options;

        PromptForMissingOptions();

        var result = await GenerateProjectsAsync();

        if ( result is not null )
        {
            await Console.Error.WriteLineAsync( result.ErrorMessage );
            return 1;
        }

        return 0;
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

    /// <summary>
    /// Generate all the project files needed for the specified options.
    /// </summary>
    /// <returns>The validation error or <c>null</c>.</returns>
    private async Task<ValidationResult?> GenerateProjectsAsync()
    {
        var csharpDirectory = $"{_options.OrganizationCode}.{_options.PluginCode}";
        var obsidianDirectory = $"{_options.OrganizationCode}.{_options.PluginCode}.Obsidian";

        if ( Path.Exists( csharpDirectory ) )
        {
            return new ValidationResult( $"Directory {csharpDirectory} already exists, aborting." );
        }

        if ( _options.Obsidian == true && Path.Exists( obsidianDirectory ) )
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
        var projectFilename = $"{_options.OrganizationCode}.{_options.PluginCode}.csproj";

        Directory.CreateDirectory( directory );

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
        var projectFilename = $"{_options.OrganizationCode}.{_options.PluginCode}.Obsidian.esproj";

        Directory.CreateDirectory( directory );

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

        var options = new CreateCommandOptions( _options );

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
                .Replace( '/', Path.DirectorySeparatorChar )
                .Replace( '\\', Path.DirectorySeparatorChar );
            var fullPath = Path.GetFullPath( Path.Combine( [.. rockWebPrefix, userPath] ) );

            options.RockWebPath = Path.GetRelativePath( Directory.GetCurrentDirectory(), fullPath );
        }

        var content = await template.RenderAsync( new TemplateContext( options ) );
        var destinationDirectory = Path.GetDirectoryName( Path.Combine( destination ) );

        if ( destinationDirectory != null )
        {
            Directory.CreateDirectory( destinationDirectory );
        }

        await File.WriteAllTextAsync( Path.Combine( destination ), content );
    }

    /// <summary>
    /// Prompts for any missing options and updates the options object.
    /// </summary>
    private void PromptForMissingOptions()
    {
        if ( string.IsNullOrEmpty( _options.Organization ) )
        {
            _options.Organization = Prompt.Input<string>( "Organization",
                placeholder: "Rock Solid Church Demo",
                validators: new[] { Validators.Required() } );
        }

        if ( string.IsNullOrEmpty( _options.OrganizationCode ) )
        {
            _options.OrganizationCode = Prompt.Input<string>( "Organization Code",
                placeholder: $"com.rocksolidchurchdemo",
                validators: new[] { Validators.Required() } );
        }

        if ( string.IsNullOrEmpty( _options.PluginName ) )
        {
            _options.PluginName = Prompt.Input<string>( "Plugin Name",
                validators: new[] { Validators.Required() } );
        }

        if ( _options.RockVersion is null )
        {
            var versionText = Prompt.Select( "Rock Version", Support.SupportedRockVersions );

            _options.RockVersion = SemVersion.Parse( versionText, SemVersionStyles.Strict );
        }

        if ( _options.RockWebPath is null )
        {
            var possibleRockWebPaths = new string[] {
                "RockWeb",
                Path.Combine( "Rock", "RockWeb" )
            };
            var defaultRockWebPath = "";

            foreach ( var p in possibleRockWebPaths )
            {
                if ( Path.Exists( Path.Combine( p, "web.config" ) ) )
                {
                    defaultRockWebPath = p;
                    break;
                }
            }

            _options.RockWebPath = Prompt.Input<string>( "Path to RockWeb",
                validators: new[] { ValidateRockWebPathPrompt } );
        }

        _options.Obsidian ??= Prompt.Confirm( "Create Obsidian Project" );

        if ( _options.Copy == null )
        {
            if ( _options.RockWebPath != null )
            {
                _options.Copy = Prompt.Confirm( "Copy artifacts to RockWeb" );
            }
            else
            {
                _options.Copy = false;
            }
        }
    }

    /// <summary>
    /// Validates the Rock version specified from the UI prompt against
    /// versions that are supported.
    /// </summary>
    /// <param name="value">The value entered in the prompt.</param>
    /// <returns>The validation error or <c>null</c> if it was valid.</returns>
    private static ValidationResult? ValidateRockWebPathPrompt( object value )
    {
        if ( value is string stringValue )
        {
            if ( !Path.Exists( stringValue ) )
            {
                return new ValidationResult( "That path does not appear to exist" );
            }

            if ( !Path.Exists( Path.Combine( stringValue, "web.config" ) ) )
            {
                return new ValidationResult( "That path does not appear to be a RockWeb path" );
            }
        }

        return ValidationResult.Success;
    }
}
