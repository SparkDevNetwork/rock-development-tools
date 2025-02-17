using System;
using System.Collections.Generic;

namespace SparkDevNetwork.Rock.CodeGenerator.DetailBlock
{
    /// <summary>
    /// Contains the options to use when generating the detail block files.
    /// </summary>
    public class DetailBlockOptions
    {
        /// <summary>
        /// The unique identifier that will be given to the block type.
        /// </summary>
        public Guid BlockTypeGuid { get; set; }

        /// <summary>
        /// The unique identifier that will be given to the EntityType backing
        /// the block type.
        /// </summary>
        public Guid BlockEntityGuid { get; set; }

        /// <summary>
        /// The C# namespace to use for the block.
        /// </summary>
        public string BlockNamespace { get; set; }

        /// <summary>
        /// The C# namespace that the model exists in.
        /// </summary>
        public string ModelNamespace { get; set; }

        /// <summary>
        /// The C# namespace to use for the view model(s) required by the block.
        /// </summary>
        public string ViewModelNamespace { get; set; }

        /// <summary>
        /// The TypeScript import path to use when importing bags for the block.
        /// </summary>
        public string TypeScriptBagImportPath { get; set; }

        /// <summary>
        /// The path to use for the C# view model bags.
        /// </summary>
        public string ViewModelCSharpRelativePath { get; set; }

        /// <summary>
        /// The path where the C# block logic will be written.
        /// </summary>
        public string CSharpBlockRelativePath { get; set; }

        /// <summary>
        /// The path to place the main obsidian file. Any additional files
        /// will be placed in a sub-folder named after the block.
        /// </summary>
        public string TypeScriptBlockRelativePath { get; set; }

        /// <summary>
        /// Gets or sets the type of the entity that will be edited by the block.
        /// </summary>
        /// <value>The type of the entity taht will be edited by the block.</value>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// Gets or sets the type of the service that will handle database access.
        /// </summary>
        /// <value>The type of the service that will handle database access.</value>
        public string ServiceTypeName { get; set; }

        /// <summary>
        /// The category to use for the block.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the properties to be included in the block.
        /// </summary>
        /// <value>The properties to be included in the block.</value>
        public List<EntityProperty> Properties { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether code for working with
        /// attribute values should be included.
        /// </summary>
        /// <value><c>true</c> if attribute values should be included; otherwise, <c>false</c>.</value>
        public bool UseAttributeValues { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether entity security should
        /// be used instead of CMS security.
        /// </summary>
        /// <value><c>true</c> if entity security should be used; otherwise, <c>false</c>.</value>
        public bool UseEntitySecurity { get; set; }

        /// <summary>
        /// Determines if this block is for a plugin or core.
        /// </summary>
        public bool IsPlugin { get; set; }
    }
}
