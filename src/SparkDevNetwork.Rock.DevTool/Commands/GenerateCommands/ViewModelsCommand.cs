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

    private readonly IGeneratorStringsProvider _stringsProvider = new GeneratorStrings();

    #region Command Options

    /// <summary>
    /// The option that defines the assembly to read.
    /// </summary>
    private readonly Argument<string> _assemblyArgument;

    /// <summary>
    /// The option that defines the output directory of the generated files.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// The option that defines if we should skip bags when generating.
    /// </summary>
    private readonly Option<bool> _noBagsOption;

    /// <summary>
    /// The option that defines if we should skip enums when generating.
    /// </summary>
    private readonly Option<bool> _noEnumsOption;

    #endregion

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

        _noBagsOption = new Option<bool>( "--no-bags", "Skip generation of bags and boxes." );
        _noEnumsOption = new Option<bool>( "--no-enums", "Skip generation of enums." );

        AddArgument( _assemblyArgument );
        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override ViewModelsCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Assembly = context.ParseResult.GetValueForArgument( _assemblyArgument ) ?? throw new Exception( "Assembly is required" );
        options.Output = context.ParseResult.GetValueForOption( _outputOption ) ?? throw new Exception( "Output is required." );
        options.NoBags = context.ParseResult.GetValueForOption( _noBagsOption );
        options.NoEnums = context.ParseResult.GetValueForOption( _noEnumsOption );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var documentationProvider = new XmlDocReader();
        var outputComponents = ExecuteOptions.Output.Split( [_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar] );
        var xmlPath = ExecuteOptions.Assembly[..^4] + ".xml";

        if ( _fs.File.Exists( xmlPath ) )
        {
            documentationProvider.ReadCommentsFrom( xmlPath );
        }

        using var mlc = GetLoadContext();
        var assembly = mlc.LoadFromAssemblyPath( ExecuteOptions.Assembly );
        var possibleTypes = GetPossibleTypes( assembly );
        var classTypeGroups = GetClassTypes( possibleTypes );
        var enumTypeGroups = GetEnumTypes( possibleTypes );

        var anyFileFailed = false;

        if ( !ExecuteOptions.NoBags )
        {
            foreach ( var classGroup in classTypeGroups )
            {
                var pathComponents = classGroup.Namespace.SkipWhile( n => n != "ViewModels" ).ToArray();
                var generator = new PluginTypeScriptGenerator( pathComponents )
                {
                    StringsProvider = _stringsProvider,
                    DocumentationProvider = documentationProvider
                };

                var content = generator.GenerateClassesViewModel( classGroup.Types );

                var path = _fs.Path.Combine( [
                    .. outputComponents,
                .. pathComponents[..^1],
                $"{classGroup.Namespace.Last().ToCamelCase()}.d.ts"
                    ] );

                if ( !WriteGeneratedFile( path, content ) )
                {
                    anyFileFailed = true;
                }
            }
        }

        if ( !ExecuteOptions.NoEnums )
        {
            foreach ( var enumGroup in enumTypeGroups )
            {
                var pathComponents = enumGroup.Namespace.SkipWhile( n => n != "Enums" ).ToArray();
                var generator = new PluginTypeScriptGenerator( pathComponents )
                {
                    StringsProvider = _stringsProvider,
                    DocumentationProvider = documentationProvider
                };

                var content = generator.GenerateEnumsViewModel( enumGroup.Types );

                var path = _fs.Path.Combine( [
                    .. outputComponents,
                .. pathComponents[..^1],
                $"{enumGroup.Namespace.Last().ToCamelCase()}.partial.ts"
                    ] );

                if ( !WriteGeneratedFile( path, content ) )
                {
                    anyFileFailed = true;
                }
            }
        }

        return Task.FromResult( anyFileFailed ? 1 : 0 );
    }

    /// <summary>
    /// Gets the context that will be used to load metadata from the assembly
    /// specified on the command line arguments. The returned instance will be
    /// configured for all runtime DLLs and other references.
    /// </summary>
    /// <returns>An instance of <see cref="MetadataLoadContext"/>.</returns>
    private MetadataLoadContext GetLoadContext()
    {
        var runtimeAssemblies = _fs.Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        var paths = new List<string>( runtimeAssemblies );
        var assemblyDirectory = _fs.Path.GetDirectoryName( ExecuteOptions.Assembly );

        paths.Add( ExecuteOptions.Assembly );

        if ( assemblyDirectory != null )
        {
            paths.AddRange( _fs.Directory.GetFiles( assemblyDirectory, "*.dll" ) );
        }

        // Create PathAssemblyResolver that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver( paths );

        return new MetadataLoadContext( resolver );
    }

    /// <summary>
    /// Get the types that might possibly fir the pattern to be exported. This
    /// will include lots of things that shouldn't be exported, but will exclude
    /// a set of common things we always want to ignore.
    /// </summary>
    /// <param name="assembly">The assembly to load types from.</param>
    /// <returns>A list of types grouped by namespace.</returns>
    private static List<NamespaceTypes> GetPossibleTypes( Assembly assembly )
    {
        // Ignore types without a namespace, or nested in a class.
        return assembly.GetTypes()
            .Where( t => !string.IsNullOrEmpty( t.Namespace ) && !t.IsNested )
            .GroupBy( t => t.Namespace! )
            .Select( grp => new NamespaceTypes
            {
                Namespace = grp.Key.Split( '.' ),
                Types = [.. grp]
            } )
            .ToList();
    }

    /// <summary>
    /// Gets the types that should be exported from the list of possible types.
    /// This performs final filtering and only returns types that are
    /// guaranteed good to export.
    /// </summary>
    /// <param name="possibleTypes">The list of types from <see cref="GetPossibleTypes"/>.</param>
    /// <returns>The list of types to export grouped by namespace.</returns>
    private static List<NamespaceTypes> GetClassTypes( List<NamespaceTypes> possibleTypes )
    {
        return possibleTypes
            .Where( a => a.Namespace.Contains( "ViewModels" ) )
            .Select( a => new NamespaceTypes
            {
                Namespace = a.Namespace,
                Types = a.Types
                    .Where( b => b.IsClass )
                    .Where( b => b.Name.EndsWith( "Bag" ) || b.Name.EndsWith( "Box" ) )
                    .ToList()
            } )
            .Where( a => a.Types.Count > 0 )
            .ToList();
    }

    /// <summary>
    /// Gets the enums that should be exported from the list of possible types.
    /// This performs final filtering and only returns types that are
    /// guaranteed good to export.
    /// </summary>
    /// <param name="possibleTypes">The list of types from <see cref="GetPossibleTypes"/>.</param>
    /// <returns>The list of enum types to export grouped by namespace.</returns>
    private static List<NamespaceTypes> GetEnumTypes( List<NamespaceTypes> possibleTypes )
    {
        return possibleTypes
            .Where( a => a.Namespace.Contains( "Enums" ) )
            .Select( a => new NamespaceTypes
            {
                Namespace = a.Namespace,
                Types = a.Types.Where( b => b.IsEnum ).ToList()
            } )
            .Where( b => b.Types.Count > 0 )
            .ToList();
    }

    /// <summary>
    /// Writes the generated file. This checks to ensure that any existing file
    /// is only overwritten if it is also a code generated file.
    /// </summary>
    /// <param name="path">The path of the file to write.</param>
    /// <param name="content">The content of the file.</param>
    /// <returns><c>true</c> if the operation was successful; otherwise <c>false</c>.</returns>
    private bool WriteGeneratedFile( string path, string content )
    {
        if ( _fs.File.Exists( path ) )
        {
            if ( !ExecuteOptions.Force )
            {
                // Strip \r characters to account for EOL differences.
                var oldContent = _fs.File.ReadAllText( path ).Replace( "\r", string.Empty );

                if ( !oldContent.StartsWith( _stringsProvider.AutoGeneratedComment.Replace( "\r", string.Empty ) ) )
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

    /// <summary>
    /// Stores a list of types for a single namespace. The namespace will have
    /// been broken out into individual components.
    /// </summary>
    private class NamespaceTypes
    {
        /// <summary>
        /// The individual components of the namespace for these types.
        /// </summary>
        public required string[] Namespace { get; init; }

        /// <summary>
        /// The types in this namespace.
        /// </summary>
        public required List<Type> Types { get; init; }
    }
}
