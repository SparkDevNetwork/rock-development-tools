using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

using GlobExpressions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.CodeGenerator;
using SparkDevNetwork.Rock.CodeGenerator.Documentation;
using SparkDevNetwork.Rock.DevTool.Data;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.Abstractions;

/// <summary>
/// Abstract command to generate a block from an entity model.
/// </summary>
abstract class BaseBlockCommand : BaseModifyCommand
{
    #region Constants

    /// <summary>
    /// The text to display for the "no selection" option.
    /// </summary>
    protected const string NoSelection = "(None)";

    #endregion

    #region Fields

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    protected IFileSystem FileSystem { get; }

    #endregion

    #region Command Options

    /// <summary>
    /// The option that defines the assembly to read.
    /// </summary>
    private readonly Option<string?> _assemblyOption;

    /// <summary>
    /// The option that defines the path to the plugin.
    /// </summary>
    private readonly Option<string?> _pluginOption;

    #endregion

    #region Properties

    /// <summary>
    /// The assembly that will be scanned for models.
    /// </summary>
    public string Assembly { get; private set; } = string.Empty;

    /// <summary>
    /// The path to the plugin that will be used when processing.
    /// </summary>
    public string PluginPath { get; private set; } = string.Empty;

    #endregion

    /// <summary>
    /// Creates a command that will handle creating a new block.
    /// </summary>
    /// <param name="name">The primary name of the action.</param>
    /// <param name="description">The description of what the command will do.</param>
    /// <param name="serviceProvider">The provider of all the required services.</param>
    public BaseBlockCommand( string name, string description, IServiceProvider serviceProvider )
        : base( name, description, serviceProvider )
    {
        FileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _pluginOption = new Option<string?>( "--plugin", "The path to the plugin files." );
        _assemblyOption = new Option<string?>( "--assembly", "The assembly to read C# models from." );

        AddOption( _assemblyOption );
        AddOption( _pluginOption );
    }

    /// <inheritdoc/>
    protected override void GetOptions( InvocationContext context )
    {
        base.GetOptions( context );

        Assembly = context.ParseResult.GetValueForOption( _assemblyOption ) ?? string.Empty;
        PluginPath = context.ParseResult.GetValueForOption( _pluginOption ) ?? FileSystem.Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets the full path to the assembly to read the models from. This will
    /// try to auto-detect if it isn't specified in the options.
    /// </summary>
    /// <param name="plugin">The parsed plugin data that is being processed.</param>
    /// <returns>The full path to the assembly or <c>null</c> if  it could not be determined.</returns>
    protected string? GetAssemblyPath( PluginData plugin )
    {
        if ( Assembly.Length > 0 )
        {
            return Assembly;
        }

        var pluginCode = plugin.Name.Replace( " ", string.Empty );
        var pluginFullName = $"{plugin.Organization.Code}.{pluginCode}";

        var files = Glob.Files( PluginPath, $"{pluginFullName}/bin/**/{pluginFullName}.dll" );

        return files
            .Select( f => FileSystem.Path.Combine( PluginPath, f ) )
            .Select( f => new
            {
                File = f,
                FileInfo = FileSystem.FileInfo.New( f )
            } )
            .OrderByDescending( f => f.FileInfo.LastWriteTime )
            .Select( f => f.File )
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the context that will be used to load metadata from the assembly
    /// specified on the command line arguments. The returned instance will be
    /// configured for all runtime DLLs and other references.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly that will be loaded.</param>
    /// <returns>An instance of <see cref="MetadataLoadContext"/>.</returns>
    protected MetadataLoadContext GetLoadContext( string assemblyPath )
    {
        var runtimeAssemblies = FileSystem.Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        var paths = new List<string>( runtimeAssemblies );
        var assemblyDirectory = FileSystem.Path.GetDirectoryName( assemblyPath );

        paths.Add( assemblyPath );

        if ( assemblyDirectory != null )
        {
            paths.AddRange( FileSystem.Directory.GetFiles( assemblyDirectory, "*.dll" ) );
        }

        // Create PathAssemblyResolver that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver( paths );

        return new MetadataLoadContext( resolver );
    }

    /// <summary>
    /// Get the types that are rock entities.
    /// </summary>
    /// <param name="assembly">The assembly to load types from.</param>
    /// <returns>A collection of types.</returns>
    protected static IEnumerable<Type> GetEntityTypes( Assembly assembly )
    {
        // Ignore types without a namespace, or nested in a class.
        return assembly.GetTypes()
            .Where( t => t.IsPublic
                && !t.IsNested
                && !t.IsAbstract )
            .Where( t =>
            {
                // This allows us to check types that depend on external
                // assemblies that are not available.
                try
                {
                    return t.IsRockEntity();
                }
                catch
                {
                    return false;
                }
            } );
    }

    /// <summary>
    /// Gets the entity type that is selected to generate a list block for.
    /// </summary>
    /// <param name="entityTypes">The possible model types that can be generated.</param>
    /// <returns>The <see cref="Type"/> that will be used for generation.</returns>
    protected Type GetEntityType( IEnumerable<Type> entityTypes )
    {
        var promptText = "Select the entity type to build the list block for:";
        var prompt = new SelectionPrompt<Type>()
            .AddChoices( entityTypes )
            .Title( promptText )
            .UseConverter( t => t.Name );

        var selection = Console.Prompt( prompt );

        Console.WriteLine( $"{promptText} {selection.Name}" );

        return selection;
    }

    /// <summary>
    /// Prompts for a boolean yes/no response from the user.
    /// </summary>
    /// <param name="title">The prompt title.</param>
    /// <param name="defaultValue">The default value if they press enter.</param>
    /// <returns><c>true</c> if the prompt was confirmed; otherwise <c>false</c>.</returns>
    protected bool GetBoolean( string title, bool defaultValue = true )
    {
        var prompt = new ConfirmationPrompt( title )
        {
            DefaultValue = defaultValue
        };

        return Console.Prompt( prompt );
    }

    /// <summary>
    /// Prompts for a string of text from the user.
    /// </summary>
    /// <param name="title">The title of the prompt.</param>
    /// <param name="defaultValue">The default value if the user presses enter.</param>
    /// <returns>The text entered on the prompt.</returns>
    protected string GetText( string title, string defaultValue )
    {
        var prompt = new TextPrompt<string>( title )
            .DefaultValue( defaultValue );

        return Console.Prompt( prompt );
    }

    /// <summary>
    /// Gets the entity type that is defined on the model.
    /// </summary>
    /// <param name="entityType">The entity model type we are processing.</param>
    /// <returns>The unique identifier or <c>null</c> if it wasn't defined.</returns>
    protected static Guid? GetEntityTypeGuid( Type entityType )
    {
        var attribute = entityType.GetCustomAttributeData( "Rock.SystemGuid.EntityTypeGuidAttribute" );

        if ( attribute == null )
        {
            return null;
        }

        return Guid.TryParse( attribute.ConstructorArguments[0].Value.ToStringSafe(), out var guid )
            ? guid
            : null;
    }

    /// <summary>
    /// Gets the C# namespace to use when generating the block.
    /// </summary>
    /// <param name="plugin">The data that describes the plugin.</param>
    /// <returns>A C# namespace string.</returns>
    protected string GetBlockNamespace( PluginData plugin )
    {
        var defaultValue = $"{plugin.Organization.Code}.{plugin.Name.Replace( " ", "" )}.Blocks";
        var prompt = new TextPrompt<string>( "C# Block Namespace" )
            .DefaultValue( defaultValue );

        return Console.Prompt( prompt );
    }

    /// <summary>
    /// Gets the C# namespace to use when generating the view model.
    /// </summary>
    /// <param name="plugin">The data that describes the plugin.</param>
    /// <returns>A C# namespace string.</returns>
    protected string GetViewModelNamespace( PluginData plugin )
    {
        var defaultValue = $"{plugin.Organization.Code}.{plugin.Name.Replace( " ", "" )}.ViewModels";
        var prompt = new TextPrompt<string>( "C# ViewModel Namespace" )
            .DefaultValue( defaultValue );

        return Console.Prompt( prompt );
    }

    /// <summary>
    /// Gets the UI category to put the block in.
    /// </summary>
    /// <param name="plugin">The data that describes the plugin.</param>
    /// <returns>A string that represents the category.</returns>
    protected string GetBlockCategory( PluginData plugin )
    {
        var defaultValue = $"{plugin.Organization.Name} > {plugin.Name}";
        var prompt = new TextPrompt<string>( "Block Category" )
            .DefaultValue( defaultValue );

        return Console.Prompt( prompt );
    }

    /// <summary>
    /// Gets a documentation provider to use when generating files. If no XML
    /// documentation file exists then an empty reader will be returned.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly that was read.</param>
    /// <returns>An instance of <see cref="XmlDocReader"/> configured to read the XML documentation.</returns>
    protected XmlDocReader GetDocumentationProvider( string assemblyPath )
    {
        var documentationProvider = new XmlDocReader();
        var xmlPath = assemblyPath[..^4] + ".xml";

        if ( FileSystem.File.Exists( xmlPath ) )
        {
            documentationProvider.ReadCommentsFrom( xmlPath );
        }

        return documentationProvider;
    }

    /// <summary>
    /// Writes the generated files to disk. If there are any files that would
    /// be replaced then we first verify with the user that they want to
    /// overwrite them.
    /// </summary>
    /// <param name="files">The generated files to be written.</param>
    /// <returns><c>true</c> if the files were written; otherwise <c>false</c>.</returns>
    protected bool WriteGeneratedFiles( ICollection<GeneratedFile> files )
    {
        var filesExist = files
            .Select( f => FileSystem.Path.Combine( PluginPath, f.SolutionRelativePath ) )
            .Where( f => FileSystem.File.Exists( f ) )
            .ToList();

        // If any files already exist, verify that they will be replaced.
        if ( filesExist.Count > 0 )
        {
            Console.WriteLine();
            Console.MarkupLine( $"[red]The following generated files already exist and will be replaced:[/]" );

            foreach ( var f in filesExist )
            {
                Console.WriteLine( FileSystem.Path.GetFriendlyPath( f ) );
            }

            Console.WriteLine();

            if ( !GetBoolean( "Replace files", false ) )
            {
                return false;
            }
        }

        foreach ( var file in files )
        {
            var filePath = FileSystem.Path.Combine( PluginPath, file.SolutionRelativePath );
            WriteFile( filePath, file.Content );
        }

        return true;
    }
}
