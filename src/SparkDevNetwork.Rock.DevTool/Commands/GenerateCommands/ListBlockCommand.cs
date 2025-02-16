using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;

using GlobExpressions;

using Microsoft.Extensions.DependencyInjection;

using SparkDevNetwork.Rock.CodeGenerator;
using SparkDevNetwork.Rock.CodeGenerator.Documentation;
using SparkDevNetwork.Rock.CodeGenerator.ListBlock;
using SparkDevNetwork.Rock.DevTool.Data;
using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// Command to generate a list block from an entity model.
/// </summary>
class ListBlockCommand : Abstractions.BaseModifyCommand
{
    #region Constants

    /// <summary>
    /// The text to display for the "no selection" option.
    /// </summary>
    private const string NoSelection = "(None)";

    #endregion

    #region Fields

    /// <summary>
    /// The object that will be used to access the filesystem.
    /// </summary>
    private readonly IFileSystem _fs;

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
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// The path to the plugin that will be used when processing.
    /// </summary>
    public string PluginPath { get; set; } = string.Empty;

    #endregion

    /// <summary>
    /// Creates a command that will handle creating a new Rock plugin.
    /// </summary>
    /// <param name="serviceProvider">The provider of all the required services.</param>
    public ListBlockCommand( IServiceProvider serviceProvider )
        : base( "listblock", "Creates a list block from a C# data model.", serviceProvider )
    {
        _fs = serviceProvider.GetRequiredService<IFileSystem>();

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
        PluginPath = context.ParseResult.GetValueForOption( _pluginOption ) ?? _fs.Directory.GetCurrentDirectory();
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var plugin = PluginInstallation.ReadPlugin( PluginPath, _fs );

        if ( plugin == null )
        {
            Console.MarkupLine( "[red]Plugin was not found.[/]" );
            return Task.FromResult( 1 );
        }

        var assemblyPath = GetAssemblyPath( plugin );

        if ( assemblyPath == null || !_fs.File.Exists( assemblyPath ) )
        {
            Console.MarkupLine( "[red]Unable to locate plugin assembly, specify with --assembly parameter.[/]" );
            return Task.FromResult( 1 );
        }

        Console.WriteLine( $"Reading models from '{_fs.Path.GetFriendlyPath( assemblyPath )}'." );

        using var mlc = GetLoadContext( assemblyPath );
        var assembly = mlc.LoadFromAssemblyPath( assemblyPath );
        var options = GetBlockOptions( assembly, plugin );

        if ( options == null )
        {
            return Task.FromResult( 1 );
        }

        var generator = new ListBlockGenerator( new FluidTemplateRenderer() )
        {
            DocumentationProvider = GetDocumentationProvider( assemblyPath )
        };

        var files = generator.GenerateFiles( options );

        var filesExist = files
            .Select( f => _fs.Path.Combine( PluginPath, f.SolutionRelativePath ) )
            .Where( f => _fs.File.Exists( f ) )
            .ToList();

        // If any files already exist, verify that they will be replaced.
        if ( filesExist.Count > 0 )
        {
            Console.WriteLine();
            Console.MarkupLine( $"[red]The following generated files already exist and will be replaced:[/]" );

            foreach ( var f in filesExist )
            {
                Console.WriteLine( f );
            }

            Console.WriteLine();

            if ( !GetBoolean( "Replace files", false ) )
            {
                return Task.FromResult( 1 );
            }
        }

        foreach ( var file in files )
        {
            var filePath = _fs.Path.Combine( PluginPath, file.SolutionRelativePath );
            _fs.Directory.CreateDirectory( _fs.Path.GetDirectoryName( filePath )! );
            _fs.File.WriteAllText( filePath, file.Content );
        }

        Console.WriteLine();
        Console.WriteLine( "Files have been generated. Remember to build the C# project and then generate the Obsidian viewmodels." );

        return Task.FromResult( 0 );
    }

    /// <summary>
    /// Gets the full path to the assembly to read the models from. This will
    /// try to auto-detect if it isn't specified in the options.
    /// </summary>
    /// <param name="plugin">The parsed plugin data that is being processed.</param>
    /// <returns>The full path to the assembly or <c>null</c> if  it could not be determined.</returns>
    private string? GetAssemblyPath( PluginData plugin )
    {
        if ( Assembly.Length > 0 )
        {
            return Assembly;
        }

        var pluginCode = plugin.Name.Replace( " ", string.Empty );
        var pluginFullName = $"{plugin.Organization.Code}.{pluginCode}";

        var files = Glob.Files( PluginPath, $"{pluginFullName}/bin/**/{pluginFullName}.dll" );

        return files
            .Select( f => _fs.Path.Combine( PluginPath, f ) )
            .Select( f => new
            {
                File = f,
                FileInfo = _fs.FileInfo.New( f )
            } )
            .OrderByDescending( f => f.FileInfo.LastWriteTime )
            .Select( f => f.File )
            .FirstOrDefault();
    }

    /// <summary>
    /// Generates all the options required to generate the block. This includes
    /// in prompting the user for all the details.
    /// </summary>
    /// <param name="assembly">The assembly that contains the entity models.</param>
    /// <param name="plugin">The data for the plugin being processed.</param>
    /// <returns>An instance of <see cref="ListBlockOptions"/> or <c>null</c> if generation was cancelled.</returns>
    private ListBlockOptions? GetBlockOptions( Assembly assembly, PluginData plugin )
    {
        var entityTypes = GetEntityTypes( assembly );
        var entityType = GetEntityType( entityTypes );
        var entityTypeGuid = GetEntityTypeGuid( entityType );
        var pluginFullName = $"{plugin.Organization.Code}.{plugin.Name.Replace( " ", string.Empty )}";
        var csharpBlockPathDefault = _fs.Path.Combine( pluginFullName, "Blocks" );
        var csharpViewModelPathDefault = _fs.Path.Combine( pluginFullName, "ViewModels" );
        var obsidianBlockPathDefault = _fs.Path.Combine( $"{pluginFullName}.Obsidian", "src" );

        if ( !entityTypeGuid.HasValue )
        {
            Console.MarkupLine( $"[red]Entity is missing EntityTypeGuid attribute.[/]" );
            return null;
        }

        var properties = GetEntityProperties( entityType );
        var tooltipSource = GetTooltipSource( entityType );
        var blockNamespace = GetBlockNamespace( plugin );
        var blockCategory = GetBlockCategory( plugin );
        var viewModelNamespace = GetViewModelNamespace( plugin );
        var csharpBlockPath = GetText( "C# block path", csharpBlockPathDefault );
        var csharpViewModelPath = GetText( "C# view model path", csharpViewModelPathDefault );
        var obsidianBlockPath = GetText( "Obsidian block path", obsidianBlockPathDefault );
        var showDelete = GetBoolean( "Show delete button" );
        var showReorder = entityType.ImplementsInterface( "Rock.Data.IOrdered" )
            && GetBoolean( "Show re-order button" );
        var showSecurity = GetBoolean( "Show security button" );
        var useEntitySecurity = GetBoolean( "Use entity security" );

        return new ListBlockOptions
        {
            BlockEntityGuid = Guid.NewGuid(),
            BlockNamespace = blockNamespace,
            BlockTypeGuid = Guid.NewGuid(),
            Category = blockCategory,
            Columns = [.. properties.Select( pi => new EntityColumn( pi ) )],
            CSharpBlockRelativePath = csharpBlockPath,
            EntityTypeGuid = entityTypeGuid.Value,
            EntityTypeName = entityType.Name,
            ExpectedRowCount = null,
            IsPlugin = true,
            ModelNamespace = entityType.Namespace,
            ServiceTypeName = $"{entityType.Name}Service",
            ShowDelete = showDelete,
            ShowReorder = showReorder,
            ShowSecurity = showSecurity,
            ToolTipSource = tooltipSource,
            TypeScriptBagImportPath = "./viewModels",
            TypeScriptBlockRelativePath = obsidianBlockPath,
            UseAttributeValues = entityType.ImplementsInterface( "Rock.Attribute.IHasAttributes" ),
            UseEntitySecurity = useEntitySecurity,
            UseIsSystem = entityType.GetProperty( "IsSystem" ) != null,
            ViewModelCSharpRelativePath = csharpViewModelPath,
            ViewModelNamespace = viewModelNamespace
        };
    }

    /// <summary>
    /// Gets the context that will be used to load metadata from the assembly
    /// specified on the command line arguments. The returned instance will be
    /// configured for all runtime DLLs and other references.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly that will be loaded.</param>
    /// <returns>An instance of <see cref="MetadataLoadContext"/>.</returns>
    private MetadataLoadContext GetLoadContext( string assemblyPath )
    {
        var runtimeAssemblies = _fs.Directory.GetFiles( RuntimeEnvironment.GetRuntimeDirectory(), "*.dll" );
        var paths = new List<string>( runtimeAssemblies );
        var assemblyDirectory = _fs.Path.GetDirectoryName( assemblyPath );

        paths.Add( assemblyPath );

        if ( assemblyDirectory != null )
        {
            paths.AddRange( _fs.Directory.GetFiles( assemblyDirectory, "*.dll" ) );
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
    private static IEnumerable<Type> GetEntityTypes( Assembly assembly )
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
    private Type GetEntityType( IEnumerable<Type> entityTypes )
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

    private List<PropertyInfo> GetEntityProperties( Type entityType )
    {
        var promptText = "Select the properties to create columns for:";
        var properties = ListBlockGenerator.GetEntityProperties( entityType, false )
            .OrderBy( pi => pi.Name );

        var prompt = new MultiSelectionPrompt<PropertyInfo>()
            .AddChoices( properties )
            .Title( promptText )
            .UseConverter( pi => pi.Name );

        var selection = Console.Prompt( prompt );

        Console.WriteLine( $"{promptText} {string.Join( ", ", selection.Select( pi => pi.Name ) )}" );

        return selection;
    }

    /// <summary>
    /// Prompts for a boolean yes/no response from the user.
    /// </summary>
    /// <param name="title">The prompt title.</param>
    /// <param name="defaultValue">The default value if they press enter.</param>
    /// <returns><c>true</c> if the prompt was confirmed; otherwise <c>false</c>.</returns>
    private bool GetBoolean( string title, bool defaultValue = true )
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
    private string GetText( string title, string defaultValue )
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
    private static Guid? GetEntityTypeGuid( Type entityType )
    {
        var attribute = entityType.GetCustomAttributeData( "Rock.SystemGuid.EntityTypeGuid" );

        if ( attribute == null )
        {
            return null;
        }

        return Guid.TryParse( attribute.ConstructorArguments[0].Value.ToStringSafe(), out var guid )
            ? guid
            : null;
    }

    /// <summary>
    /// Gets the source column to use for the grid tooltip data.
    /// </summary>
    /// <param name="entityType">The entity model type we are processing.</param>
    /// <returns>The property name to use for the tooltip or <c>null</c> to disable the tooltip.</returns>
    private string? GetTooltipSource( Type entityType )
    {
        var promptText = "Select the property that will be used for row tooltip:";
        var stringProperties = ListBlockGenerator.GetEntityProperties( entityType, false )
            .Where( pi => pi.PropertyType.FullName == typeof( string ).FullName )
            .OrderBy( pi => pi.Name )
            .Select( pi => pi.Name );

        var prompt = new SelectionPrompt<string>()
            .Title( promptText );

        prompt.AddChoice( NoSelection );
        prompt.AddChoices( stringProperties );

        var choice = Console.Prompt( prompt );

        Console.WriteLine( $"{promptText} {choice}" );

        return choice == NoSelection ? null : choice;
    }

    /// <summary>
    /// Gets the C# namespace to use when generating the block.
    /// </summary>
    /// <param name="plugin">The data that describes the plugin.</param>
    /// <returns>A C# namespace string.</returns>
    private string GetBlockNamespace( PluginData plugin )
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
    private string GetViewModelNamespace( PluginData plugin )
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
    private string GetBlockCategory( PluginData plugin )
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
    private XmlDocReader GetDocumentationProvider( string assemblyPath )
    {
        var documentationProvider = new XmlDocReader();
        var xmlPath = assemblyPath[..^4] + ".xml";

        if ( _fs.File.Exists( xmlPath ) )
        {
            documentationProvider.ReadCommentsFrom( xmlPath );
        }

        return documentationProvider;
    }
}
