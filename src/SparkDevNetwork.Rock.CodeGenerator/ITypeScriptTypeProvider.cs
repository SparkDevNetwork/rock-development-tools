using System;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// A provider for custom <see cref="TypeScriptTypeDefinition"/> objects
    /// that will represent C# runtime types in TypeScript.
    /// </summary>
    public interface ITypeScriptTypeProvider
    {
        /// <summary>
        /// Gets the TypeScript type definition of the C# type.
        /// </summary>
        /// <param name="type">The C# runtime type.</param>
        /// <returns>An instance of <see cref="TypeScriptTypeDefinition"/> that represents the type and any imports.</returns>
        TypeScriptTypeDefinition GetTypeScriptTypeDefinition( Type type, bool isRequired );
    }
}
