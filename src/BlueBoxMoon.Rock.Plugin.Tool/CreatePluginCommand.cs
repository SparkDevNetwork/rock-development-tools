using System.ComponentModel.DataAnnotations;

using Fluid;

using Sharprompt;

namespace BlueBoxMoon.Rock.Plugin.Tool;

/// <summary>
/// Handler for the Create command.
/// </summary>
class CreatePluginCommand
{
    /// <summary>
    /// The options that have been resolved for this command.
    /// </summary>
    private readonly CreateCommandOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="CreatePluginCommand"/>. 
    /// </summary>
    /// <param name="options">The options parsed from the command line.</param>
    public CreatePluginCommand( CreateCommandOptions options )
    {
        _options = options;
    }

    /// <summary>
    /// Invokes the command and creates the plugin projects.
    /// </summary>
    /// <returns>The exit code for the process.</returns>
    public async Task<int> InvokeAsync()
    {
        PromptForMissingOptions();

        var result = await GenerateProjectsAsync();

        if ( result is not null )
        {
            await Console.Error.WriteLineAsync( result.ErrorMessage );
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
        await CopyTemplateAsync( "CSharp.gitignore", [directory, ".gitignore"]);

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

        await CopyTemplateAsync( "Obsidian.project.esproj", [directory, projectFilename] );
        await CopyTemplateAsync( "Obsidian.package.json", [directory, "package.json"] );
        await CopyTemplateAsync( "Obsidian.rollup.config.cjs", [directory, "rollup.config.cjs"]);
        await CopyTemplateAsync( "Obsidian.gitignore", [directory, ".gitignore"]);

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
        using var stream = GetType().Assembly.GetManifestResourceStream( $"BlueBoxMoon.Rock.Plugin.Tool.Templates.{sourceName}" )
            ?? throw new ArgumentOutOfRangeException( nameof( sourceName ), $"Template {sourceName} not found" );
        using var textStream = new StreamReader( stream );
        var rawContent = await textStream.ReadToEndAsync();
        var parser = new FluidParser();

        if ( !parser.TryParse( rawContent, out var template, out var error ) )
        {
            throw new Exception( error );
        }

        var options = _options.Clone();

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

            _options.RockVersion = new Version( versionText );
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
