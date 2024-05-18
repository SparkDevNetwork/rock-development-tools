using Fluid;

using Semver;

using Sharprompt;

using Spectre.Console;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace SparkDevNetwork.Rock.Plugin.Tool.Commands.PluginCommands;

/// <summary>
/// Command handler for <see cref="CreateCommand"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options used by the command.</typeparam>
class CreateCommandHandler : Abstractions.BaseModifyCommandHandler<CreateCommandOptions>
{
    /// <summary>
    /// Creates the action command handler.
    /// </summary>
    /// <param name="options">The options for this handler.</param>
    protected CreateCommandHandler( CreateCommandOptions options )
        : base( options )
    {
    }

    /// <inheritdoc/>
    public override async Task<int> InvokeAsync()
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
        var csharpDirectory = $"{Options.OrganizationCode}.{Options.PluginCode}";
        var obsidianDirectory = $"{Options.OrganizationCode}.{Options.PluginCode}.Obsidian";

        if ( Path.Exists( csharpDirectory ) )
        {
            return new ValidationResult( $"Directory {csharpDirectory} already exists, aborting." );
        }

        if ( Options.Obsidian == true && Path.Exists( obsidianDirectory ) )
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
        var projectFilename = $"{Options.OrganizationCode}.{Options.PluginCode}.csproj";

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
        var projectFilename = $"{Options.OrganizationCode}.{Options.PluginCode}.Obsidian.esproj";

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

        var options = new CreateCommandOptions( Options );

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
        if ( string.IsNullOrEmpty( Options.Organization ) )
        {
            Options.Organization = Prompt.Input<string>( "Organization",
                placeholder: "Rock Solid Church Demo",
                validators: new[] { Validators.Required() } );
        }

        if ( string.IsNullOrEmpty( Options.OrganizationCode ) )
        {
            Options.OrganizationCode = Prompt.Input<string>( "Organization Code",
                placeholder: $"com.rocksolidchurchdemo",
                validators: new[] { Validators.Required() } );
        }

        if ( string.IsNullOrEmpty( Options.PluginName ) )
        {
            Options.PluginName = Prompt.Input<string>( "Plugin Name",
                validators: new[] { Validators.Required() } );
        }

        if ( Options.RockVersion is null )
        {
            var versionText = Prompt.Select( "Rock Version", Support.SupportedRockVersions );

            Options.RockVersion = SemVersion.Parse( versionText, SemVersionStyles.Strict );
        }

        if ( Options.RockWebPath is null )
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

            Options.RockWebPath = Prompt.Input<string>( "Path to RockWeb",
                validators: new[] { ValidateRockWebPathPrompt } );
        }

        Options.Obsidian ??= Prompt.Confirm( "Create Obsidian Project" );

        if ( Options.Copy == null )
        {
            if ( Options.RockWebPath != null )
            {
                Options.Copy = Prompt.Confirm( "Copy artifacts to RockWeb" );
            }
            else
            {
                Options.Copy = false;
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
