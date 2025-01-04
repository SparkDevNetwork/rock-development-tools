using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Represents a TypeScript type definition and required imports.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TypeScriptTypeDefinition
    {
        /// <summary>
        /// The definition, such as <c>string | null</c>.
        /// </summary>
        public string Definition { get; }

        /// <summary>
        /// The imports required for the definition to work.
        /// </summary>
        public IEnumerable<TypeScriptImport> Imports { get; }

        /// <summary>
        /// Creates a new instance of <see cref="TypeScriptTypeDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition, such as <c>string | null</c>.</param>
        /// <param name="imports">The imports required for the definition to work.</param>
        public TypeScriptTypeDefinition( string definition, IEnumerable<TypeScriptImport> imports )
        {
            Definition = definition;
            Imports = imports;
        }
    }
}
