using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.CodeGenerator;
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
    public ViewModelsCommand( IServiceProvider serviceProvider )
        : base( "viewmodels", "Generates TypeScript enum definitions.", serviceProvider )
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
    protected override ViewModelsCommandOptions GetOptions( InvocationContext context )
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
                if ( t.IsClass && !t.IsNested )
                {
                    var components = !string.IsNullOrWhiteSpace( t.Namespace )
                        ? t.Namespace.Split( '.' )
                        : [];

                    if ( !components.Contains( "ViewModels" ) )
                    {
                        continue;
                    }

                    if ( !t.Name.EndsWith( "Bag" ) && !t.Name.EndsWith( "Box" ) )
                    {
                        continue;
                    }

                    components = components.SkipWhile( c => c != "ViewModels" ).ToArray();

                    var gen = new PluginTypeScriptGenerator( components )
                    {
                        StringsProvider = new GeneratorStrings(),
                        DocumentationProvider = xml
                    };

                    var content = gen.GenerateClassViewModel( t );

                    var path = _fs.Path.Combine( [.. outputComponents, .. components.Skip( 1 ), $"{t.Name.ToCamelCase()}.d.ts"] );

                    Console.WriteLine( $"{path}:" );
                    Console.Write( content );
                }
            }
        }

        return Task.FromResult( 0 );
    }
}
