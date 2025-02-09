using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator.DetailBlock
{
    public class DetailBlockGenerator
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
        /// Creates a new instance of <see cref="DetailBlockGenerator"/>. 
        /// </summary>
        /// <param name="renderer">The renderer to use when generating the source code files from templates.</param>
        public DetailBlockGenerator( ITemplateRenderer renderer )
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
            return entityType.GetProperties( BindingFlags.Public | BindingFlags.Instance )
                .Where( p => p.GetCustomAttributeData( "System.Runtime.Serialization.DataMemberAttribute" ) != null
                    || p.PropertyType.IsRockEntity() )
                .Where( p => p.GetCustomAttributeData( "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute" ) == null )
                .Where( p => !_systemProperties.Contains( p.Name ) )
                .Where( p => includeAdvancedProperties || !_advancedProperties.Contains( p.Name ) );
        }

        /// <summary>
        /// Gets all the entity properties without applying any filtering.
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
        /// Determines if the property has a valid type that can be used during
        /// automatic code generation.
        /// </summary>
        /// <param name="property">The property to be validated.</param>
        /// <returns><c>true</c> if the property type is known and valid, <c>false</c> otherwise.</returns>
        public static bool IsPropertyValid( PropertyInfo property )
        {
            return EntityProperty.IsSupportedPropertyType( property.PropertyType );
        }

        /// <summary>
        /// Generates all the files required to make a skeleton detail block
        /// for the selected entity type.
        /// </summary>
        /// <param name="options">The options that describe all the options to use when generating code.</param>
        /// <returns>A collection of <see cref="GeneratedFile"/> objects that represent the files to be created or updated.</returns>
        public IList<GeneratedFile> GenerateFiles( DetailBlockOptions options )
        {
            var files = new List<GeneratedFile>();
            // var domain = options.EntityType.GetCustomAttribute<Data.RockDomainAttribute>()?.Name ?? "Unknown";
            // var domainNamespace = CoreSupport.GetDomainFolderName( domain );
            // var bagPath = $"Rock.ViewModels\\Blocks\\{domainNamespace}\\{options.EntityType.Name}Detail";
            // var blockPath = $"Rock.Blocks\\{domainNamespace}";
            // var typeScriptBlockPath = $"Rock.JavaScript.Obsidian.Blocks\\src\\{domainNamespace}";
            // var bagNamespace = $"Rock.ViewModels.Blocks.{domainNamespace}.{options.EntityType.Name}Detail";
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
                ["BlockNamespace"] = options.BlockNamespace, // Rock.Blocks.{{ DomainNamespace }}
                ["ViewModelNamespace"] = options.ViewModelNamespace, //Rock.ViewModels.Blocks.{{ DomainNamespace }}.{{ EntityName }}Detail
                ["TypeScriptBagImportPath"] = options.TypeScriptBagImportPath, // @Obsidian/ViewModels/Blocks/{{ DomainNamespace }}/{{ EntityName }}Detail
                ["EntityName"] = options.EntityTypeName,
                ["ServiceName"] = options.ServiceTypeName,
                ["Category"] = options.Category,
                ["Properties"] = options.Properties,
                ["IsPlugin"] = options.IsPlugin,
                ["UseAttributeValues"] = options.UseAttributeValues,
                ["UseDescription"] = options.Properties.Any( p => p.Name == "Description" ),
                ["UseEntitySecurity"] = options.UseEntitySecurity,
                ["UseIsActive"] = options.Properties.Any( p => p.Name == "IsActive" ),
                ["UseIsSystem"] = options.Properties.Any( p => p.Name == "IsSystem" ),
                ["UseOrder"] = options.Properties.Any( p => p.Name == "Order" ),
                ["UseName"] = options.Properties.Any( p => p.Name == "Name" )
            };

            // Generate the <Entity>Bag.cs file.
            var content = generator.GenerateEntityBag( options.EntityTypeName, options.ViewModelNamespace, options.Properties, $"The item details for the {options.EntityTypeName.SplitCase()} Detail block." );
            files.Add( new GeneratedFile( $"{options.EntityTypeName}Bag.cs", options.ViewModelCSharpRelativePath, content ) );

            // Generate the <Entity>DetailOptionsBag.cs file.
            content = generator.GenerateOptionsBag( $"{options.EntityTypeName}DetailOptionsBag", options.ViewModelNamespace, $"The additional configuration options for the {options.EntityTypeName.SplitCase()} Detail block." );
            files.Add( new GeneratedFile( $"{options.EntityTypeName}DetailOptionsBag.cs", options.ViewModelCSharpRelativePath, content ) );

            // Generate the main <Entity>Detail.cs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EntityDetailBlock-cs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"{options.EntityTypeName}Detail.cs", options.CSharpBlockRelativePath, result ) );
            }

            // Generate the Obsidian <entity>Detail.obs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EntityDetailBlock-obs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetObsidianCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"{options.EntityTypeName.ToCamelCase()}Detail.obs", options.TypeScriptBlockRelativePath, result ) );
            }

            // Generate the Obsidian <Entity>Detail\viewPanel.partial.obs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.ViewPanel-partial-obs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetObsidianCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"viewPanel.partial.obs", $"{options.TypeScriptBlockRelativePath}\\{options.EntityTypeName}Detail", result ) );
            }

            // Generate the Obsidian <Entity>Detail\editPanel.partial.obs file.
            using ( var reader = new StreamReader( GetType().Assembly.GetManifestResourceStream( "SparkDevNetwork.Rock.CodeGenerator.Resources.EditPanel-partial-obs.txt" ) ) )
            {
                var lavaTemplate = reader.ReadToEnd();

                var result = GetObsidianCopyrightComment()
                    + _templateRenderer.Render( lavaTemplate, mergeFields );

                files.Add( new GeneratedFile( $"editPanel.partial.obs", $"{options.TypeScriptBlockRelativePath}\\{options.EntityTypeName}Detail", result ) );
            }

            // Generate the Obsidian <Entity>Detail\types.partial.ts file.
            content = tsGenerator.GenerateDetailBlockTypeDefinitionFile( new Dictionary<string, string>
            {
                ["ParentPage"] = "ParentPage"
            } );
            files.Add( new GeneratedFile( "types.partial.ts", $"{options.TypeScriptBlockRelativePath}\\{options.EntityTypeName}Detail", content ) );

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
