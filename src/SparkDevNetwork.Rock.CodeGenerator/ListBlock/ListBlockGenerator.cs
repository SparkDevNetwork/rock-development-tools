using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator.ListBlock
{
    public class ListBlockGenerator : Generator
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
        private IEnumerable<PropertyInfo> GetProperties( Type entityType, bool includeAdvancedProperties )
        {
            var properties = entityType?.GetProperties( BindingFlags.Public | BindingFlags.Instance )
                .Where( p => p.GetCustomAttributeData( "System.Runtime.Serialization.DataMemberAttribute" ) != null
                    || p.PropertyType.ImplementsInterface( "Rock.Data.IEntity" ) )
                .Where( p => p.GetCustomAttributeData( "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute" ) == null )
                .Where( p => !_systemProperties.Contains( p.Name ) )
                .Where( p => includeAdvancedProperties || !_advancedProperties.Contains( p.Name ) )
                .OrderBy( p => p.Name )
                .ToList()
                ?? new List<PropertyInfo>();

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
        public bool IsPropertyValidForColumn( PropertyInfo property )
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
                || property.PropertyType.ImplementsInterface( "Rock.Data.IEntity" );
        }

        /// <summary>
        /// Gets all the entity properties without applying any filtering.
        /// </summary>
        /// <param name="entityType">Type of the entity whose properties should be enumerated.</param>
        /// <param name="includeAdvancedProperties">If <c>true</c> then advanced properties will be included.</param>
        /// <returns>An enumeration of all the entity's properties.</returns>
        public IEnumerable<PropertyInfo> GetEntityProperties( Type entityType, bool includeAdvancedProperties )
        {
            return GetProperties( entityType, includeAdvancedProperties )
                .OrderBy( p => p.Name )
                .ToList();
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
                StringsProvider = StringsProvider
            };
            var tsGenerator = new TypeScriptViewModelGenerator
            {
                StringsProvider = StringsProvider
            };

            // Create the standard merge fields that will be used by the Lava engine
            // when generating all the files.
            var mergeFields = new Dictionary<string, object>
            {
                ["BlockTypeGuid"] = options.BlockTypeGuid,
                ["BlockEntityGuid"] = options.BlockEntityGuid,
                ["BlockNamespace"] = options.BlockNamespace,
                ["ViewModelNamespace"] = options.ViewModelNamespace,
                ["TypeScriptBagImportPath"] = options.TypeScriptBagImportPath,
                ["EntityName"] = options.EntityTypeName,
                ["ServiceName"] = options.ServiceTypeName,
                ["EntityTypeGuid"] = options.EntityTypeGuid,
                ["Category"] = options.Category,
                ["Columns"] = options.Columns,
                ["GridImports"] = options.Columns.SelectMany( c => c.GridImports ).Distinct().OrderBy( i => i ).ToList(),
                ["IsPlugin"] = options.IsPlugin,
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

                var result = _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"{options.EntityTypeName}List.cs", options.CSharpBlockRelativePath, result ) );
            }

            // Generate the Obsidian <entity>List.obs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EntityListBlock-obs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = _templateRenderer.Render( lavaTemplate, mergeFields );

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

        #endregion
    }
}
