using System;
using System.Collections.Generic;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Contains the options to use when generating the list block files.
    /// </summary>
    public class ListBlockOptions
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
        /// The type of the entity that will be edited by the block.
        /// </summary>
        public string EntityTypeName { get; set; }

        /// <summary>
        /// The type of the service that will handle database access.
        /// </summary>
        public string ServiceTypeName { get; set; }

        /// <summary>
        /// The unique identifier of the entity type.
        /// </summary>
        public Guid EntityTypeGuid { get; set; }

        /// <summary>
        /// The category to use for the block.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The properties to be included in the block.
        /// </summary>
        public List<EntityColumn> Columns { get; set; }

        /// <summary>
        /// Determines if the IsSystem property should be used to provide
        /// additional logic checks.
        /// </summary>
        public bool UseIsSystem { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether code for working with
        /// attribute values should be included.
        /// </summary>
        public bool UseAttributeValues { get; set; }

        /// <summary>
        /// Determines if entity security should be used instead of CMS
        /// security.
        /// </summary>
        public bool UseEntitySecurity { get; set; }

        /// <summary>
        /// The tool tip source field.
        /// </summary>
        public string ToolTipSource { get; set; }

        /// <summary>
        /// Determines whether to show the reorder column.
        /// </summary>
        public bool ShowReorder { get; set; }

        /// <summary>
        /// Determines whether to show the security column.
        /// </summary>
        public bool ShowSecurity { get; set; }

        /// <summary>
        /// Determines whether to show the delete column.
        /// </summary>
        public bool ShowDelete { get; set; }

        /// <summary>
        /// The expected row count.
        /// </summary>
        public int? ExpectedRowCount { get; set; }

        /// <summary>
        /// Determines if this block is for a plugin or core.
        /// </summary>
        public bool IsPlugin { get; set; }
    }
}
