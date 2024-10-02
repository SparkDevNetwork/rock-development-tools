using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.CodeGenerator;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// Command to generate enum TypeScript files from one or more C# assemblies.
/// </summary>
partial class EnumsCommand : Abstractions.BaseModifyCommand<EnumsCommandOptions>
{
    private readonly IFileSystem _fs;

    /// <summary>
    /// The option that defines the assemblies to read.
    /// </summary>
    private readonly Argument<List<string>> _assembliesArgument;

    /// <summary>
    /// The option that defines the namespaces to read.
    /// </summary>
    private readonly Option<List<string>> _namespacesOption;

    /// <summary>
    /// The option that defines the output directory of the generated files.
    /// </summary>
    private readonly Option<string?> _outputOption;

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    public EnumsCommand( IServiceProvider serviceProvider )
        : base( "enums", "Generates TypeScript enum definitions.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

        _assembliesArgument = new Argument<List<string>>( "assembly", "The assembly to read C# enums from." );

        _namespacesOption = new Option<List<string>>( "--namespace", "The root namespace to scan for enums." );
        _namespacesOption.AddAlias( "--ns" );

        _outputOption = new Option<string?>( "--output", "Location to place the generated output." );
        _outputOption.AddAlias( "-o" );
        _outputOption.IsRequired = true;

        AddArgument( _assembliesArgument );
        AddOption( _namespacesOption );
        AddOption( _outputOption );
    }

    /// <inheritdoc/>
    protected override EnumsCommandOptions GetOptions( InvocationContext context )
    {
        var options = base.GetOptions( context );

        options.Assemblies = context.ParseResult.GetValueForArgument( _assembliesArgument ) ?? [];
        options.Namespaces = context.ParseResult.GetValueForOption( _namespacesOption ) ?? [];
        options.Output = context.ParseResult.GetValueForOption( _outputOption ) ?? throw new Exception( "Output is required." );

        return options;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var xml = new CodeGenerator.Documentation.XmlDocReader();
        var runtimeAssemblies = _fs.Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        var paths = new List<string>( runtimeAssemblies );

        foreach ( var assemblyPath in ExecuteOptions.Assemblies )
        {
            var xmlPath = assemblyPath[..^4] + ".xml";

            if ( _fs.File.Exists( xmlPath ) )
            {
                xml.ReadCommentsFrom( xmlPath );
            }

            paths.Add( assemblyPath );

            var assemblyDirectory = _fs.Path.GetDirectoryName( assemblyPath );

            if ( assemblyDirectory != null )
            {
                paths.AddRange( _fs.Directory.GetFiles( assemblyDirectory, "*.dll" ) );
            }
        }

        paths.AddRange( ExecuteOptions.Assemblies );

        // Create PathAssemblyResolver that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver( paths );
        using var mlc = new MetadataLoadContext( resolver );

        var outputComponents = ExecuteOptions.Output.Split( [_fs.Path.DirectorySeparatorChar, _fs.Path.AltDirectorySeparatorChar] );

        foreach ( var assemblyPath in ExecuteOptions.Assemblies )
        {
            var assembly = mlc.LoadFromAssemblyPath( assemblyPath );

            foreach ( var t in assembly.GetTypes() )
            {
                if ( t.IsEnum )
                {
                    var typeNamespace = t.Namespace ?? string.Empty;

                    if ( ExecuteOptions.Namespaces.Count > 0 )
                    {
                        var ns = ExecuteOptions.Namespaces.FirstOrDefault( ns => typeNamespace.StartsWith( ns ) );

                        if ( ns == null )
                        {
                            continue;
                        }

                        typeNamespace = typeNamespace[ns.Length..].TrimStart( '.' );
                    }

                    var gen = new TypeScriptViewModelGenerator
                    {
                        StringsProvider = new GeneratorStrings(),
                        TypeProvider = new TypeProvider(),
                        DocumentationProvider = xml
                    };
                    var content = gen.GenerateEnumViewModel( t );

                    var components = typeNamespace.Split( '.' );

                    if ( ExecuteOptions.Namespaces.Count == 0 && components.Contains( "Enums" ) )
                    {
                        components = components.SkipWhile( c => c != "Enums" ).Skip( 1 ).ToArray();
                    }

                    var path = _fs.Path.Combine( [.. outputComponents, .. components, $"{t.Name.ToCamelCase()}.partial.ts"] );

                    Console.WriteLine( $"{path}:" );
                    Console.Write( content );
                }
                // else if ( t.Name.EndsWith( "Bag" ) )
                // {
                //     var gen = new TypeScriptViewModelGenerator( new GeneratorStrings(), new TypeProvider(), xml );
                //     var content = gen.GenerateTypeViewModel( t );

                //     Console.Write( content );
                // }
            }
        }

        return Task.FromResult( 0 );
    }




    class GeneratorStrings : IGeneratorStringsProvider
    {
        public string AutoGeneratedComment => "// This code was auto-generated, any manual changes made will be lost.\n";

        public string CopyrightComment => "// No copyright.\n\n";
    }

    class TypeProvider : ITypeScriptTypeProvider
    {
        public TypeScriptTypeDefinition? GetTypeScriptTypeDefinition( Type type, bool isRequired )
        {
            if ( type.Namespace == "tester" && type.Name.EndsWith( "Bag" ) )
            {
                var path = $"../{type.Name.ToCamelCase()}"; //$"{type.Namespace.Substring(15).Trim('.').Replace('.', '/')}/{type.Name.ToCamelCase()}";

                var imports = new List<TypeScriptImport>
                {
                    new TypeScriptImport
                    {
                        SourcePath = $"{path}",
                        NamedImport = type.Name
                    }
                };

                return new TypeScriptTypeDefinition( type.Name, imports );
            }
            else if ( type.Namespace == "tester" && type.IsEnum )
            {
                var path = $"../{type.Name.ToCamelCase()}";

                var imports = new List<TypeScriptImport>
                {
                    new TypeScriptImport
                    {
                        SourcePath = $"{path}",
                        NamedImport = type.Name
                    }
                };

                return new TypeScriptTypeDefinition( type.Name, imports );
            }

            return null;
        }
    }
}
