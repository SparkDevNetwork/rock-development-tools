using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Provides methods for generating specific C# files.
    /// </summary>
    public class CSharpViewModelGenerator : Generator
    {
        #region Properties

        /// <summary>
        /// The provider for documentation text on types and methods.
        /// </summary>
        public IDocumentationProvider DocumentationProvider { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Generates an empty options bag.
        /// </summary>
        /// <param name="bagName">Name of the bag.</param>
        /// <param name="bagNamespace">The namespace the bag will be placed in.</param>
        /// <param name="xmlDoc">The XML documentation text for the class.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateOptionsBag( string bagName, string bagNamespace, string xmlDoc )
        {
            var sb = new StringBuilder();
            sb.AppendLine( "    /// <summary>" );
            sb.AppendLine( $"    /// {xmlDoc}" );
            sb.AppendLine( "    /// </summary>" );
            sb.AppendLine( $"    public class {bagName}" );
            sb.AppendLine( "    {" );
            sb.AppendLine( "    }" );

            return GenerateCSharpFile( new string[0], bagNamespace, sb.ToString(), false );
        }

        /// <summary>
        /// Generates the entity bag from a set of properties.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="bagNamespace">The namespace the bag will be placed in.</param>
        /// <param name="properties">The properties that will be contained in the bag.</param>
        /// <param name="xmlDoc">The XML documentation text for the class.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateEntityBag( string entityName, string bagNamespace, List<EntityProperty> properties, string xmlDoc )
        {
            var usings = new List<string>
            {
                "Rock.ViewModels.Utility"
            };

            var sb = new StringBuilder();
            sb.AppendLine( "    /// <summary>" );
            sb.AppendLine( $"    /// {xmlDoc}" );
            sb.AppendLine( "    /// </summary>" );
            sb.AppendLine( $"    public class {entityName}Bag : EntityBagBase" );
            sb.AppendLine( "    {" );

            var sortedProperties = properties.OrderBy( p => p.Name ).ToList();

            // Loop through the sorted list of properties and emit each one.
            for ( int i = 0; i < sortedProperties.Count; i++ )
            {
                var property = sortedProperties[i];

                if ( i > 0 )
                {
                    sb.AppendLine();
                }

                var declaration = GetCSharpPropertyTypeDeclaration( property.PropertyType );

                usings.AddRange( declaration.RequiredUsings );

                AppendCommentBlock( sb, property.PropertyInfo, 8 );
                sb.AppendLine( $"        public {declaration.TypeName} {property.Name} {{ get; set; }}" );
            }

            sb.AppendLine( "    }" );

            return GenerateCSharpFile( usings, bagNamespace, sb.ToString(), false );
        }

        /// <summary>
        /// Gets the C# property type declaration.
        /// </summary>
        /// <param name="type">The type that will need to be declared.</param>
        /// <returns>a <see cref="PropertyDeclaration"/> that represents the property.</returns>
        private static PropertyDeclaration GetCSharpPropertyTypeDeclaration( Type type )
        {
            // If the type is a collection of entities then use a collection
            // of ListItemBag objects.
            if ( type.IsGenericType && type.GenericTypeArguments.Length == 1 )
            {
                if ( type.ImplementsInterface( typeof( ICollection<> ).FullName ) )
                {
                    return new PropertyDeclaration( $"List<ListItemBag>", new[] { "System.Collections.Generic", "Rock.ViewModels.Utility" } );
                }
            }

            // If the type is an entity then use a ListItemBag object.
            if ( type.ImplementsInterface( "Rock.Data.IEntity" ) )
            {
                return new PropertyDeclaration( $"ListItemBag", new[] { "Rock.ViewModels.Utility" } );
            }

            // Try for a primitive property type.
            return type.GetCSharpPropertyDeclaration();
        }

        /// <summary>
        /// Appends the comment block for the member to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the comment to.</param>
        /// <param name="memberInfo">The member information to get comments for.</param>
        /// <param name="indentationSize">Size of the indentation for the comment block.</param>
        [ExcludeFromCodeCoverage]
        private void AppendCommentBlock( StringBuilder sb, MemberInfo memberInfo, int indentationSize )
        {
            var xdoc = DocumentationProvider?.GetMemberComments( memberInfo )?.Summary?.PlainText;

            AppendCommentBlock( sb, xdoc, indentationSize );
        }

        /// <summary>
        /// Appends the comment block to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the comment to.</param>
        /// <param name="comment">The comment to append.</param>
        /// <param name="indentationSize">Size of the indentation for the comment block.</param>
        private void AppendCommentBlock( StringBuilder sb, string comment, int indentationSize )
        {
            if ( comment.IsNullOrWhiteSpace() )
            {
                return;
            }

            comment = comment.Replace( "\r\n", $"\r\n{new string( ' ', indentationSize )}/// " );

            sb.AppendLine( $"{new string( ' ', indentationSize )}/// <summary>" );
            sb.AppendLine( $"{new string( ' ', indentationSize )}/// {comment}" );
            sb.AppendLine( $"{new string( ' ', indentationSize )}/// </summary>" );
        }

        #endregion
    }
}
