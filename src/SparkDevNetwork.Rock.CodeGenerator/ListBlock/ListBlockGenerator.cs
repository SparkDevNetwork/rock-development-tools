using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.ListBlock
{
    public class ListBlockGenerator
    {
        #region Fields

        /// <summary>
        /// The properties that are considered system level and always excluded.
        /// </summary>
        private static readonly string[] _systemProperties = new[]
        {
            "Attributes",
            "AttributeValues",
            "Guid",
            "Id"
        };

        /// <summary>
        /// The properties that are considered advanced and will only show up
        /// if they are specifically requested.
        /// </summary>
        private static readonly string[] _advancedProperties = new[]
        {
            "CreatedByPersonAlias",
            "CreatedByPersonAliasId",
            "CreatedDateTime",
            "ForeignGuid",
            "ForeignId",
            "ForeignKey",
            "ModifiedByPersonAlias",
            "ModifiedByPersonAliasId",
            "ModifiedDateTime"
        };

        /// <summary>
        /// The renderer to use when generating the source code files from
        /// templates.
        /// </summary>
        private readonly ITemplateRenderer _templateRenderer;

        #endregion

        #region Properties

        /// <summary>
        /// The provider for various strings used during C# and TypeScript
        /// file code generation.
        /// </summary>
        public IGeneratorStringsProvider StringsProvider { get; set; }

        /// <summary>
        /// The provider for various strings used during Obsidian file code generation.
        /// </summary>
        public IGeneratorStringsProvider ObsidianStringsProvider { get; set; }

        /// <summary>
        /// The provider for documentation text on types and methods.
        /// </summary>
        public IDocumentationProvider DocumentationProvider { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="ListBlockGenerator"/>. 
        /// </summary>
        /// <param name="renderer">The renderer to use when generating the source code files from templates.</param>
        public ListBlockGenerator( ITemplateRenderer renderer )
        {
            _templateRenderer = renderer;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the properties that exist on the specified entity type. This automatically
        /// handles filtering out system and advanced properties.
        /// </summary>
        /// <param name="entityType">Type of the entity whose properties are to be enumerated.</param>
        /// <param name="includeAdvancedProperties">If <c>true</c> then advanced properties will be included.</param>
        /// <returns>An enumeration of the valid properties that match the filtering options.</returns>
        private static IEnumerable<PropertyInfo> GetProperties( Type entityType, bool includeAdvancedProperties )
        {
            var properties = entityType.GetProperties( BindingFlags.Public | BindingFlags.Instance )
                .Where( p => p.GetCustomAttributeData( "System.Runtime.Serialization.DataMemberAttribute" ) != null
                    || p.PropertyType.IsRockEntity() )
                .Where( p => p.GetCustomAttributeData( "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute" ) == null )
                .Where( p => !_systemProperties.Contains( p.Name ) )
                .Where( p => includeAdvancedProperties || !_advancedProperties.Contains( p.Name ) )
                .ToList();

            // Filter out any EntityId properties if we have a navigation
            // property to the entity.
            if ( !includeAdvancedProperties )
            {
                properties = properties
                    .Where( p => !p.Name.EndsWith( "Id" )
                        || !properties.Any( p2 => p2.Name == p.Name.Substring( 0, p.Name.Length - 2 ) ) )
                    .ToList();
            }

            return properties;
        }

        /// <summary>
        /// Gets the properties that are valid to use when generating columns.
        /// </summary>
        /// <param name="properties">The properties that exist on the entity.</param>
        /// <returns>A set of <see cref="PropertyItem"/> objects.</returns>
        public static bool IsPropertyValidForColumn( PropertyInfo property )
        {
            return property.PropertyType.FullName == typeof( string ).FullName
                || property.PropertyType.FullName == typeof( Guid ).FullName
                || property.PropertyType.FullName == typeof( Guid? ).FullName
                || property.PropertyType.FullName == typeof( bool ).FullName
                || property.PropertyType.FullName == typeof( bool? ).FullName
                || property.PropertyType.FullName == typeof( int ).FullName
                || property.PropertyType.FullName == typeof( int? ).FullName
                || property.PropertyType.FullName == typeof( decimal ).FullName
                || property.PropertyType.FullName == typeof( decimal? ).FullName
                || property.PropertyType.FullName == typeof( float ).FullName
                || property.PropertyType.FullName == typeof( float? ).FullName
                || property.PropertyType.FullName == typeof( double ).FullName
                || property.PropertyType.FullName == typeof( double? ).FullName
                || property.PropertyType.IsRockEntity();
        }

        /// <summary>
        /// Gets all the supported entity properties.
        /// </summary>
        /// <param name="entityType">Type of the entity whose properties should be enumerated.</param>
        /// <param name="includeAdvancedProperties">If <c>true</c> then advanced properties will be included.</param>
        /// <returns>An enumeration of all the entity's properties.</returns>
        public static IEnumerable<PropertyInfo> GetEntityProperties( Type entityType, bool includeAdvancedProperties )
        {
            if ( entityType == null )
            {
                throw new ArgumentNullException( nameof( entityType ) );
            }

            return GetProperties( entityType, includeAdvancedProperties )
                .OrderBy( p => p.Name );
        }

        /// <summary>
        /// Generates all the files required to make a skeleton list block
        /// for the selected entity type.
        /// </summary>
        /// <param name="options">The options that describe all the options to use when generating code.</param>
        /// <returns>A collection of <see cref="GeneratedFile"/> objects that represent the files to be created or updated.</returns>
        public IList<GeneratedFile> GenerateFiles( ListBlockOptions options )
        {
            var files = new List<GeneratedFile>();
            var generator = new CSharpViewModelGenerator
            {
                StringsProvider = StringsProvider,
                DocumentationProvider = DocumentationProvider
            };
            var tsGenerator = new TypeScriptViewModelGenerator
            {
                StringsProvider = StringsProvider,
                DocumentationProvider = DocumentationProvider
            };

            // Create the standard merge fields that will be used by the Lava engine
            // when generating all the files.
            var mergeFields = new Dictionary<string, object>
            {
                ["BlockTypeGuid"] = options.BlockTypeGuid,
                ["BlockEntityGuid"] = options.BlockEntityGuid,
                ["BlockNamespace"] = options.BlockNamespace,
                ["ModelNamespace"] = options.ModelNamespace,
                ["ViewModelNamespace"] = options.ViewModelNamespace,
                ["TypeScriptBagImportPath"] = options.TypeScriptBagImportPath,
                ["EntityName"] = options.EntityTypeName,
                ["ServiceName"] = options.ServiceTypeName,
                ["EntityTypeGuid"] = options.EntityTypeGuid,
                ["Category"] = options.Category,
                ["Columns"] = options.Columns,
                ["GridImports"] = options.Columns.SelectMany( c => c.GridImports ).Distinct().OrderBy( i => i ).ToList(),
                ["IsPlugin"] = options.IsPlugin,
                ["PluginOrganizationCode"] = options.PluginOrganizationCode,
                ["PluginOrganizationName"] = options.PluginOrganizationName,
                ["PluginName"] = options.PluginName,
                ["UseIsSystem"] = options.UseIsSystem,
                ["UseAttributeValues"] = options.UseAttributeValues,
                ["UseEntitySecurity"] = options.UseEntitySecurity,
                ["ToolTipSource"] = options.ToolTipSource,
                ["ShowReorder"] = options.ShowReorder,
                ["ShowSecurity"] = options.ShowSecurity,
                ["ShowDelete"] = options.ShowDelete,
                ["ExpectedRowCount"] = options.ExpectedRowCount
            };

            // Generate the <Entity>ListOptionsBag.cs file.
            var content = generator.GenerateOptionsBag( $"{options.EntityTypeName}ListOptionsBag", options.ViewModelNamespace, $"The additional configuration options for the {options.EntityTypeName.SplitCase()} List block." );
            files.Add( new GeneratedFile( $"{options.EntityTypeName}ListOptionsBag.cs", options.ViewModelCSharpRelativePath, content ) );

            // Generate the main <Entity>List.cs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EntityListBlock-cs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"{options.EntityTypeName}List.cs", options.CSharpBlockRelativePath, result ) );
            }

            // Generate the Obsidian <entity>List.obs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EntityListBlock-obs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetObsidianCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"{options.EntityTypeName.ToCamelCase()}List.obs", options.TypeScriptBlockRelativePath, result ) );
            }

            // Generate the Obsidian <Entity>List\types.partial.ts file.
            content = tsGenerator.GenerateListBlockTypeDefinitionFile( new Dictionary<string, string>
            {
                ["DetailPage"] = "DetailPage"
            } );
            files.Add( new GeneratedFile( "types.partial.ts", Path.Combine( options.TypeScriptBlockRelativePath, $"{options.EntityTypeName}List" ), content ) );

            return files;
        }

        /// <summary>
        /// Gets the copyright text to prepend to C# files.
        /// </summary>
        /// <returns>A string of text or an empty string.</returns>
        [ExcludeFromCodeCoverage]
        private string GetCopyrightComment()
        {
            return StringsProvider?.CopyrightComment ?? string.Empty;
        }

        /// <summary>
        /// Gets the copyright text to prepend to Obsidian files.
        /// </summary>
        /// <returns>A string of text or an empty string.</returns>
        [ExcludeFromCodeCoverage]
        private string GetObsidianCopyrightComment()
        {
            return ObsidianStringsProvider?.CopyrightComment ?? string.Empty;
        }

        #endregion
    }
}
