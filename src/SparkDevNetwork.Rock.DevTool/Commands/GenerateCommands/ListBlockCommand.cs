using System.CommandLine;
using System.Reflection;

using SparkDevNetwork.Rock.CodeGenerator;
using SparkDevNetwork.Rock.CodeGenerator.ListBlock;
using SparkDevNetwork.Rock.DevTool.Data;
using SparkDevNetwork.Rock.DevTool.DevEnvironment;

using Spectre.Console;

namespace SparkDevNetwork.Rock.DevTool.Commands.GenerateCommands;

/// <summary>
/// Command to generate a list block from an entity model.
/// </summary>
class ListBlockCommand : Abstractions.BaseBlockCommand
{
    /// <summary>
    /// Creates a command that will handle creating a new list block.
    /// </summary>
    /// <param name="serviceProvider">The provider of all the required services.</param>
    public ListBlockCommand( IServiceProvider serviceProvider )
        : base( "listblock", "Creates a list block from a C# data model.", serviceProvider )
    {
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync()
    {
        var plugin = PluginInstallation.ReadPlugin( PluginPath, FileSystem );

        if ( plugin == null )
        {
            Console.MarkupLine( "[red]Plugin was not found.[/]" );
            return Task.FromResult( 1 );
        }

        var assemblyPath = GetAssemblyPath( plugin );

        if ( assemblyPath == null || !FileSystem.File.Exists( assemblyPath ) )
        {
            Console.MarkupLine( "[red]Unable to locate plugin assembly, specify with --assembly parameter.[/]" );
            return Task.FromResult( 1 );
        }

        Console.WriteLine( $"Reading models from '{FileSystem.Path.GetFriendlyPath( assemblyPath )}'." );

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

        if ( !WriteGeneratedFiles( files ) )
        {
            return Task.FromResult( 1 );
        }

        Console.WriteLine();
        Console.WriteLine( "Files have been generated. Remember to build the C# project and then generate the Obsidian viewmodels." );

        return Task.FromResult( 0 );
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
        var csharpBlockPathDefault = FileSystem.Path.Combine( pluginFullName, "Blocks" );
        var csharpViewModelPathDefault = FileSystem.Path.Combine( pluginFullName, "ViewModels" );
        var obsidianBlockPathDefault = FileSystem.Path.Combine( $"{pluginFullName}.Obsidian", "src" );

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
            PluginName = plugin.Name,
            PluginOrganizationCode = plugin.Organization.Code,
            PluginOrganizationName = plugin.Organization.Name,
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
    /// Gets the properties that should be included in the generated block code.
    /// </summary>
    /// <param name="entityType">The type of entity the block is geing generated for.</param>
    /// <returns>A list of properties to be included.</returns>
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
}
