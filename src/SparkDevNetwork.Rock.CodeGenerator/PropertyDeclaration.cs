using System;
using System.Collections.Generic;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Represents a property declaration type as well as any C#
    /// using statements that need to be included.
    /// </summary>
    class PropertyDeclaration
    {
        #region Properties

        /// <summary>
        /// Gets the required usings statements for this type to work.
        /// </summary>
        /// <value>The required usings statements for this type to work.</value>
        public IList<string> RequiredUsings { get; }

        /// <summary>
        /// Gets the name of the type, this is the text that will be emitted to reference the type.
        /// </summary>
        /// <value>The name of the type.</value>
        public string TypeName { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDeclaration"/> class.
        /// </summary>
        /// <param name="typeName">The text to use when referencing the type.</param>
        /// <param name="requiredUsings">The required usings for the type.</param>
        public PropertyDeclaration( string typeName, IList<string> requiredUsings )
        {
            RequiredUsings = requiredUsings;
            TypeName = typeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDeclaration"/> class.
        /// </summary>
        /// <param name="typeName">The text to use when referencing the type.</param>
        public PropertyDeclaration( string typeName )
        {
            RequiredUsings = Array.Empty<string>();
            TypeName = typeName;
        }

        #endregion
    }
}
