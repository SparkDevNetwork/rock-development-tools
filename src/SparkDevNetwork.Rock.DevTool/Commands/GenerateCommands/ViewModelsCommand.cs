using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.CodeGenerator;
using SparkDevNetwork.Rock.CodeGenerator.Documentation;
using SparkDevNetwork.Rock.DevTool.Generators;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// Command to generate enum TypeScript files from one or more C# assemblies.
/// </summary>
partial class ViewModelsCommand : Abstractions.BaseModifyCommand<ViewModelsCommandOptions>
{
    private readonly IFileSystem _fs;

    /// <summary>
    /// The option that defines the assembly to read.
    /// </summary>
    private readonly Argument<string> _assemblyArgument;

    /// <summary>
    /// The option that defines the output directory of the generated files.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public ViewModelsCommand( IServiceProvider serviceProvider )
        : base( "viewmodels", "Generates TypeScript enum definitions.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _assemblyArgument = new Argument<string>( "assembly", "The assembly to read C# classes from." );

        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );
        _outputOption.IsRequired = true;

        AddArgument( _assemblyArgument );
        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override ViewModelsCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Assembly = context.ParseResult.GetValueForArgument( _assemblyArgument ) ?? throw new Exception( "Assembly is required" );
        options.Output = context.ParseResult.GetValueForOption( _outputOption ) ?? throw new Exception( "Output is required." );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var documentationProvider = new CodeGenerator.Documentation.XmlDocReader();
        var runtimeAssemblies = _fs.Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        var paths = new List<string>( runtimeAssemblies );

        var xmlPath = ExecuteOptions.Assembly[..^4] + ".xml";

        if ( _fs.File.Exists( xmlPath ) )
        {
            documentationProvider.ReadCommentsFrom( xmlPath );
        }

        paths.Add( ExecuteOptions.Assembly );

        var assemblyDirectory = _fs.Path.GetDirectoryName( ExecuteOptions.Assembly );

        if ( assemblyDirectory != null )
        {
            paths.AddRange( _fs.Directory.GetFiles( assemblyDirectory, "*.dll" ) );
        }

        // Create PathAssemblyResolver that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver( paths );
        using var mlc = new MetadataLoadContext( resolver );

        var outputComponents = ExecuteOptions.Output.Split( [_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar] );

        var assembly = mlc.LoadFromAssemblyPath( ExecuteOptions.Assembly );
        var types = assembly.GetTypes()
            .Where( t => t.IsClass && !t.IsNested )
            .Where( t => t.Name.EndsWith( "Bag" ) || t.Name.EndsWith( "Box" ) );

        foreach ( var type in types )
        {
            ProcessType( type, outputComponents, documentationProvider );
        }

        return Task.FromResult( 0 );
    }

    /// <summary>
    /// Process a single type by generating and writing to the file.
    /// </summary>
    /// <param name="type">The C# type to export.</param>
    /// <param name="outputComponents">The path components that make up the root output path.</param>
    /// <param name="documentationProvider">The provider of XML documentation.</param>
    /// <returns><c>true</c> if the operation was successful; otherwise <c>false</c>.</returns>
    private bool ProcessType( Type type, string[] outputComponents, IDocumentationProvider documentationProvider )
    {
        var components = !string.IsNullOrWhiteSpace( type.Namespace )
            ? type.Namespace.Split( '.' )
            : [];

        if ( !components.Contains( "ViewModels" ) )
        {
            return true;
        }

        components = components.SkipWhile( c => c != "ViewModels" ).ToArray();

        var gen = new PluginTypeScriptGenerator( components )
        {
            StringsProvider = new GeneratorStrings(),
            DocumentationProvider = documentationProvider
        };

        var content = gen.GenerateClassViewModel( type );
        var path = _fs.Path.Combine( [.. outputComponents, .. components.Skip( 1 ), $"{type.Name.ToCamelCase()}.d.ts"] );

        if ( _fs.File.Exists( path ) )
        {
            if ( !ExecuteOptions.Force )
            {
                var oldContent = _fs.File.ReadAllText( path );

                if ( !oldContent.StartsWith( gen.StringsProvider.AutoGeneratedComment ) )
                {
                    Console.MarkupLineInterpolated( $"[red]File '{path}' exists and may not be auto-generated, skipping. Run with --force to override.[/]" );
                    return false;
                }
            }

            WriteFile( path, content );
        }
        else
        {
            var directory = _fs.Path.GetDirectoryName( path );

            if ( directory != null && !_fs.Directory.Exists( directory ) )
            {
                CreateDirectory( directory );
            }

            WriteFile( path, content );
        }

        return true;
    }
}
