using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using SparkDevNetwork.Rock.CodeGenerator.Documentation;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Contains methods for generating specific TypeScript files.
    /// </summary>
    public class TypeScriptViewModelGenerator : Generator
    {
        #region Properties

        /// <summary>
        /// The provider for documentation text on types and methods.
        /// </summary>
        public IDocumentationProvider DocumentationProvider { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a class view model file for the C# runtime type.
        /// </summary>
        /// <param name="type">The type to be generated.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateClassViewModel( Type type )
        {
            var (content, imports) = GenerateClassViewModelContent( type );

            return GenerateTypeScriptFile( imports, content, true );
        }

        /// <summary>
        /// Generates a single view model file for the C# runtime types.
        /// </summary>
        /// <param name="types">The types to be generated.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateClassesViewModel( List<Type> types )
        {
            var sb = new StringBuilder();
            var imports = new List<TypeScriptImport>();

            foreach ( var type in types )
            {
                var (content, classImports) = GenerateClassViewModelContent( type );

                sb.Append( content );
                sb.AppendLine();

                imports.AddRange( classImports );
            }


            return GenerateTypeScriptFile( imports, sb.ToString(), true );
        }

        /// <summary>
        /// Generates the class view model for the C# runtime type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="typeComment">The type comment.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="type">The type being generated.</param>
        /// <param name="isAutoGen">if set to <c>true</c> the auto generated comment will be included.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        internal (string Content, List<TypeScriptImport> Imports) GenerateClassViewModelContent( Type type )
        {
            var typeComment = GetDocumentationSummary( type );
            var typeName = GetClassNameForType( type );
            var properties = type.GetProperties().ToList();

            var imports = new List<TypeScriptImport>();
            var sb = new StringBuilder();

            AppendCommentBlock( sb, typeComment, 0 );
            sb.AppendLine( $"export type {typeName} = {{" );

            var sortedProperties = properties.OrderBy( p => p.Name ).ToList();

            // Loop through each sorted property and emit the declaration.
            for ( int i = 0; i < sortedProperties.Count; i++ )
            {
                var property = sortedProperties[i];
                var isNullable = !IsNonNullType( property.PropertyType );

                if ( i > 0 )
                {
                    sb.AppendLine();
                }

                AppendCommentBlock( sb, property, 4 );

                sb.Append( $"    {property.Name.ToCamelCase()}" );

                // If its nullable that means it could also be undefined.
                if ( isNullable )
                {
                    sb.Append( "?" );
                }

                var valueType = GetTypeScriptTypeDefinition( property.PropertyType, !isNullable );
                sb.AppendLine( $": {valueType.Definition};" );

                imports.AddRange( valueType.Imports );
            }

            sb.AppendLine( "};" );

            // Remove recursive references to self.
            imports = imports.Where( i => i.DefaultImport != typeName && i.NamedImport != typeName ).ToList();

            return (sb.ToString(), imports);
        }

        /// <summary>
        /// Generates the view model file for an enum.
        /// </summary>
        /// <param name="type">The enumeration type.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateEnumViewModel( Type type )
        {
            var content = GenerateEnumViewModelContent( type );

            return GenerateTypeScriptFile( new List<TypeScriptImport>(), content );
        }

        /// <summary>
        /// Generates the view model file for a set of enums.
        /// </summary>
        /// <param name="types">The enumeration types.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        public string GenerateEnumsViewModel( List<Type> types )
        {
            var sb = new StringBuilder();

            foreach ( var type in types )
            {
                sb.Append( GenerateEnumViewModelContent( type ) );
                sb.AppendLine();
            }

            return GenerateTypeScriptFile( new List<TypeScriptImport>(), sb.ToString() );
        }

        /// <summary>
        /// Generates the content for a single enum type. This only includes
        /// the content of the enum itself, not any additional file content
        /// such as copyright text.
        /// </summary>
        /// <param name="type">The enum type to generate.</param>
        /// <returns>A string that contains the TypeScript code for this enum type.</returns>
        private string GenerateEnumViewModelContent( Type type )
        {
            var typeComment = GetDocumentationSummary( type );
            var typeName = GetClassNameForType( type );
            var isFlagType = type.GetCustomAttributeData( "System.FlagsAttribute" ) != null;

            var sb = new StringBuilder();

            sb.Append( GenerateEnumViewModelContent( type, false ) );
            sb.AppendLine();
            sb.Append( GenerateEnumViewModelContent( type, true ) );
            sb.AppendLine();

            AppendCommentBlock( sb, typeComment, 0 );

            if ( !isFlagType )
            {
                sb.AppendLine( $"export type {typeName} = typeof {typeName}[keyof typeof {typeName}];" );
            }
            else
            {
                sb.AppendLine( $"export type {typeName} = number;" );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates the view model file for the values of an enum.
        /// </summary>
        /// <param name="type">The enumeration type.</param>
        /// <returns>A string that contains the contents of the file.</returns>
        private string GenerateEnumViewModelContent( Type type, bool isDescription )
        {
            var typeComment = GetDocumentationSummary( type );
            var typeName = GetClassNameForType( type );
            var fields = type.GetFields( BindingFlags.Static | BindingFlags.Public ).ToList();

            var sb = new StringBuilder();

            AppendCommentBlock( sb, typeComment, 0 );

            if ( type.GetCustomAttributeData( "System.ObsoleteAttribute" ) is CustomAttributeData obsoleteTypeAttribute )
            {
                var obsoleteMessage = string.Empty;

                if ( obsoleteTypeAttribute.ConstructorArguments.Count > 0 )
                {
                    obsoleteMessage = obsoleteTypeAttribute.ConstructorArguments[0].Value.ToStringSafe();
                }

                sb.AppendLine( $"/** @deprecated {obsoleteMessage} */" );
            }

            if ( !isDescription )
            {
                sb.AppendLine( $"export const {typeName} = {{" );
            }
            else
            {
                sb.AppendLine( $"export const {typeName}Description: Record<number, string> = {{" );
            }

            //var sortedFields = fields.OrderBy( f => f.GetRawConstantValue() ).ToList();
            var sortedFields = fields.ToList();

            // Loop through each sorted field and emit the declaration.
            for ( int i = 0; i < sortedFields.Count; i++ )
            {
                var field = fields[i];
                var obsoleteFieldAttribute = field.GetCustomAttributeData( "System.ObsoleteAttribute" );

                if ( isDescription && obsoleteFieldAttribute != null )
                {
                    // If this enum value is obsolete and there is another
                    // enum that is not obsolete with the same integer
                    // value then skip this one.
                    var hasOtherField = sortedFields
                        .Any( f => ( int ) f.GetRawConstantValue() == ( int ) field.GetRawConstantValue()
                            && f.GetCustomAttributeData( "System.ObsoleteAttribute" ) == null );

                    if ( hasOtherField )
                    {
                        continue;
                    }
                }

                if ( i > 0 )
                {
                    sb.AppendLine();
                }

                var fieldName = field.Name;

                if ( !isDescription )
                {
                    AppendCommentBlock( sb, field, 4 );

                    if ( obsoleteFieldAttribute != null )
                    {
                        var obsoleteMessage = string.Empty;

                        if ( obsoleteFieldAttribute.ConstructorArguments.Count > 0 && obsoleteFieldAttribute.ConstructorArguments[0].ArgumentType.FullName == "System.String" )
                        {
                            obsoleteMessage = obsoleteFieldAttribute.ConstructorArguments[0].Value.ToStringSafe();
                        }

                        sb.AppendLine( $"    /** @deprecated {obsoleteMessage} */" );
                    }

                    if ( type.GetCustomAttributeData( "System.FlagsAttribute" ) != null )
                    {
                        sb.Append( $"    {fieldName}: 0x{( int ) field.GetRawConstantValue():X4}" );
                    }
                    else
                    {
                        sb.Append( $"    {fieldName}: {field.GetRawConstantValue()}" );
                    }
                }
                else
                {
                    fieldName = field.GetRawConstantValue().ToString();

                    if ( fieldName[0] == '-' )
                    {
                        fieldName = $"[{fieldName}]";
                    }

                    if ( field.GetCustomAttributeData( "System.ComponentModel.DescriptionAttribute" ) is CustomAttributeData fieldDescriptionAttribute )
                    {
                        var description = fieldDescriptionAttribute.ConstructorArguments[0].Value.ToString();

                        sb.Append( $"    {fieldName}: \"{description}\"" );
                    }
                    else
                    {
                        sb.Append( $"    {fieldName}: \"{field.Name.SplitCase()}\"" );
                    }
                }

                if ( i + 1 < sortedFields.Count )
                {
                    sb.AppendLine( "," );
                }
                else
                {
                    sb.AppendLine();
                }
            }

            if ( isDescription )
            {
                sb.AppendLine( "};" );
            }
            else
            {
                sb.AppendLine( "} as const;" );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates the file for a SystemGuid declaration.
        /// </summary>
        /// <param name="type">The SystemGuid type.</param>
        /// <returns>A string that contains the file contents.</returns>
        public string GenerateSystemGuidForType( Type type )
        {
            // Get all the values to be included.
            var values = type.GetFields( BindingFlags.Static | BindingFlags.Public )
                .OrderBy( f => f.Name )
                .Select( f => new
                {
                    Field = f,
                    Value = ( string ) f.GetValue( null )
                } );

            var camelName = $"{type.Name.Substring( 0, 1 ).ToLower()}{type.Name.Substring( 1 )}";
            var typeComment = GetDocumentationSummary( type );

            var sb = new StringBuilder();

            AppendCommentBlock( sb, typeComment, 0 );
            sb.AppendLine( $"export const {type.Name} = {{" );

            // Loop through each value and emit the declaration.
            foreach ( var value in values )
            {
                bool convertToUpper = true;
                string name = string.Empty;

                // Convert the name into a JavaScript friendly one.
                for ( int i = 0; i < value.Field.Name.Length; i++ )
                {
                    if ( convertToUpper )
                    {
                        name += value.Field.Name[i].ToString().ToUpper();
                        convertToUpper = false;
                    }
                    else
                    {
                        if ( value.Field.Name[i] == '_' )
                        {
                            convertToUpper = true;
                        }
                        else
                        {
                            name += value.Field.Name[i].ToString().ToLower();
                        }
                    }
                }

                AppendCommentBlock( sb, value.Field, 4 );

                sb.AppendLine( $"    {name}: \"{value.Value}\"," );
            }

            sb.AppendLine( "};" );

            return GenerateTypeScriptFile( new List<TypeScriptImport>(), sb.ToString() );
        }

        /// <summary>
        /// Generates the detail block type definition file for any declared types.
        /// </summary>
        /// <param name="navigationUrlKeys">The navigation URL keys to include.</param>
        /// <returns>A string that contains the file contents.</returns>
        public string GenerateDetailBlockTypeDefinitionFile( Dictionary<string, string> navigationUrlKeys )
        {
            var imports = new List<TypeScriptImport>();

            var sb = new StringBuilder();

            sb.AppendLine( $"export const enum NavigationUrlKey {{" );

            var sortedItems = navigationUrlKeys.OrderBy( k => k.Key ).ToList();

            // Loop through each navigation key and emit the declaration.
            for ( int i = 0; i < sortedItems.Count; i++ )
            {
                var item = sortedItems[i];

                if ( i > 0 )
                {
                    sb.AppendLine();
                }

                sb.Append( $"    {item.Key} = \"{item.Value}\"" );

                if ( i + 1 < sortedItems.Count )
                {
                    sb.Append( "," );
                }

                sb.AppendLine();
            }

            sb.AppendLine( "}" );

            return GenerateTypeScriptFile( imports, sb.ToString(), false );
        }

        /// <summary>
        /// Generates the detail block type definition file for any declared types.
        /// </summary>
        /// <param name="navigationUrlKeys">The navigation URL keys to include.</param>
        /// <returns>A string that contains the file contents.</returns>
        public string GenerateListBlockTypeDefinitionFile( Dictionary<string, string> navigationUrlKeys )
        {
            var imports = new List<TypeScriptImport>();

            var sb = new StringBuilder();

            sb.AppendLine( $"export const enum NavigationUrlKey {{" );

            var sortedItems = navigationUrlKeys.OrderBy( k => k.Key ).ToList();

            // Loop through each navigation key and emit the declaration.
            for ( int i = 0; i < sortedItems.Count; i++ )
            {
                var item = sortedItems[i];

                if ( i > 0 )
                {
                    sb.AppendLine();
                }

                sb.Append( $"    {item.Key} = \"{item.Value}\"" );

                if ( i + 1 < sortedItems.Count )
                {
                    sb.Append( "," );
                }

                sb.AppendLine();
            }

            sb.AppendLine( "}" );

            return GenerateTypeScriptFile( imports, sb.ToString(), false );
        }

        /// <summary>
        /// Appends the comment block for the member to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the comment to.</param>
        /// <param name="memberInfo">The member information to get comments for.</param>
        /// <param name="indentationSize">Size of the indentation for the comment block.</param>
        private void AppendCommentBlock( StringBuilder sb, MemberInfo memberInfo, int indentationSize )
        {
            var xdoc = GetDocumentationSummary( memberInfo );

            AppendCommentBlock( sb, xdoc, indentationSize );
        }

        /// <summary>
        /// Appends the comment block to the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to append the comment to.</param>
        /// <param name="comment">The comment to append.</param>
        /// <param name="indentationSize">Size of the indentation for the comment block.</param>
        /// /// <param name="sourceType">The source type that this comment is related to, if a method this would be the type that contains the method.</param>
        internal static void AppendCommentBlock( StringBuilder sb, string comment, int indentationSize )
        {
            if ( string.IsNullOrWhiteSpace( comment ) )
            {
                return;
            }

            // Replace any XML code with backticks.
            comment = comment.Replace( "<c>", "`" ).Replace( "</c>", "`" );

            // Replace any self closing see tags.
            comment = Regex.Replace( comment, "<see\\s+cref=\"([^\"]+)\"\\s*\\/>", m =>
            {
                if ( m.Groups[1].Value.Length < 2 || m.Groups[1].Value[1] != ':' )
                {
                    return m.Groups[1].Value;
                }

                var segments = m.Groups[1].Value.Substring( 2 ).Split( '.' );

                if ( segments.Length < 2 )
                {
                    return m.Groups[1].Value;
                }

                if ( m.Groups[1].Value[0] == 'F' )
                {
                    // This should be an enum, so don't change case.
                    return $"{{@link {string.Join( ".", segments.TakeLast( 2 ) )}}}";
                }
                else
                {
                    var refTypeName = segments[segments.Length - 2];
                    var refName = segments[segments.Length - 1];

                    return $"{{@link {refTypeName}.{refName.ToCamelCase()}}}";
                }
            } );

            // Replace any non self closing see tags.
            comment = Regex.Replace( comment, "<see\\s+cref=\"[^\"]+\"\\s*>([^<]*)<\\/see>", m =>
            {
                return m.Groups[1].Value;
            } );

            // If it contains newline information then insert it as a block.
            if ( comment.Contains( "\r\n" ) )
            {
                // Paragraph breaks come in as 3 newline pairs, make it just 2.
                comment = comment.Replace( "\r\n\r\n\r\n", "\r\n\r\n" );

                comment = comment.Replace( "\r\n", $"\r\n{new string( ' ', indentationSize )} * " );

                sb.AppendLine( $"{new string( ' ', indentationSize )}/**" );
                sb.AppendLine( $"{new string( ' ', indentationSize )} * {comment}" );
                sb.AppendLine( $"{new string( ' ', indentationSize )} */" );
            }
            else
            {
                sb.AppendLine( $"{new string( ' ', indentationSize )}/** {comment} */" );
            }
        }

        /// <summary>
        /// Determines whether the type is a non-nullable type.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns><c>true</c> if the type is non-nullable; otherwise, <c>false</c>.</returns>
        private static bool IsNonNullType( Type type )
        {
            return type.IsPrimitive
                || type.IsEnum
                || type.FullName == typeof( decimal ).FullName
                || type.FullName == typeof( Guid ).FullName;
        }

        /// <summary>
        /// Gets the TypeScript definition type of the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="isRequired">Indicates if this type is already considered required.
        /// <returns>A string that contains the definition, such as "boolean | null".</returns>
        protected virtual TypeScriptTypeDefinition GetTypeScriptTypeDefinition( Type type, bool isRequired )
        {
            var imports = new List<TypeScriptImport>();
            var isNullable = false;

            if ( type.IsGenericType && type.GetGenericTypeDefinition().FullName == typeof( Nullable<> ).FullName )
            {
                type = type.GetGenericArguments()[0];
                isNullable = true;
            }

            // Default to "unknown" type
            var tsType = "unknown";

            // Determine if this is a numeric type.
            var isNumeric = type.FullName == typeof( byte ).FullName
                || type.FullName == typeof( sbyte ).FullName
                || type.FullName == typeof( short ).FullName
                || type.FullName == typeof( ushort ).FullName
                || type.FullName == typeof( int ).FullName
                || type.FullName == typeof( uint ).FullName
                || type.FullName == typeof( long ).FullName
                || type.FullName == typeof( ulong ).FullName
                || type.FullName == typeof( decimal ).FullName
                || type.FullName == typeof( float ).FullName
                || type.FullName == typeof( double ).FullName;

            if ( type.FullName == typeof( bool ).FullName )
            {
                tsType = "boolean";
            }
            else if ( isNumeric )
            {
                tsType = "number";
            }
            else if ( type.FullName == typeof( string ).FullName )
            {
                tsType = "string";
                isNullable = !isRequired;
            }
            else if ( type.FullName == typeof( DateTime ).FullName || type.FullName == typeof( DateTimeOffset ).FullName )
            {
                tsType = "string";
                isNullable = isNullable || !isRequired;
            }
            else if ( type.FullName == typeof( Guid ).FullName )
            {
                tsType = "Guid";
                imports.Add( new TypeScriptImport
                {
                    SourcePath = "@Obsidian/Types",
                    NamedImport = "Guid"
                } );
                isNullable = isNullable || !isRequired;
            }
            else if ( type.IsGenericParameter )
            {
                tsType = type.Name;
                isNullable = isNullable || !isRequired;
            }
            else if ( type.IsArray )
            {
                var tsBaseType = GetTypeScriptTypeDefinition( type.GetElementType(), true );

                tsType = $"{tsBaseType.Definition}[]";
                imports.AddRange( tsBaseType.Imports );
                isNullable = !isRequired;
            }
            else if ( type.IsGenericType )
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if ( genericTypeDefinition.FullName == typeof( Dictionary<,> ).FullName )
                {
                    var keyType = GetTypeScriptTypeDefinition( type.GetGenericArguments()[0], true );
                    var valueType = GetTypeScriptTypeDefinition( type.GetGenericArguments()[1], true );

                    tsType = $"Record<{keyType.Definition}, {valueType.Definition}>";
                    imports.AddRange( keyType.Imports );
                    imports.AddRange( valueType.Imports );
                    isNullable = !isRequired;
                }
                else if ( type.ImplementsInterface( typeof( ICollection<> ).FullName ) )
                {
                    var valueType = GetTypeScriptTypeDefinition( type.GenericTypeArguments[0], true );

                    tsType = $"{valueType.Definition}[]";
                    imports.AddRange( valueType.Imports );
                    isNullable = !isRequired;
                }
                else if ( genericTypeDefinition.FullName == "Rock.ViewModels.Utility.ValidPropertiesBox`1" )
                {
                    var valueType = GetTypeScriptTypeDefinition( type.GetGenericArguments()[0], true );

                    tsType = $"ValidPropertiesBox<{valueType.Definition}>";
                    imports.AddRange( valueType.Imports );
                    imports.Add( new TypeScriptImport
                    {
                        SourcePath = "@Obsidian/ViewModels/Utility/validPropertiesBox",
                        NamedImport = "ValidPropertiesBox"
                    } );
                    isNullable = !isRequired;
                }
            }
            else if ( type.Namespace?.StartsWith( "Rock.ViewModels" ) == true && ( type.Name.EndsWith( "Bag" ) || type.Name.EndsWith( "Box" ) ) )
            {
                var path = $"{type.Namespace.Substring( 15 ).Trim( '.' ).Replace( '.', '/' )}/{type.Name.ToCamelCase()}";
                tsType = type.Name;
                imports.Add( new TypeScriptImport
                {
                    SourcePath = $"@Obsidian/ViewModels/{path}",
                    NamedImport = type.Name
                } );
                isNullable = !isRequired;
            }
            else if ( type.IsEnum )
            {
                if ( type.Namespace?.StartsWith( "Rock.Enums" ) == true )
                {
                    var path = $"{type.Namespace.Substring( 10 ).Trim( '.' ).Replace( '.', '/' )}/{type.Name.ToCamelCase()}";
                    tsType = type.Name;
                    imports.Add( new TypeScriptImport
                    {
                        SourcePath = $"@Obsidian/Enums/{path}",
                        NamedImport = type.Name
                    } );
                }
                else if ( type.GetCustomAttributeData( "Rock.Enums.EnumDomainAttribute" ) != null )
                {
                    var domainAttribute = type.GetCustomAttributeData( "Rock.Enums.EnumDomainAttribute" );
                    var domain = domainAttribute.ConstructorArguments[0].Value.ToString();
                    var path = $"{CoreSupport.GetDomainFolderName( domain )}/{type.Name.ToCamelCase()}";
                    tsType = type.Name;
                    imports.Add( new TypeScriptImport
                    {
                        SourcePath = $"@Obsidian/Enums/{path}",
                        NamedImport = type.Name
                    } );
                }
                else
                {
                    tsType = "number";
                }
            }

            if ( isNullable )
            {
                return new TypeScriptTypeDefinition( $"{tsType} | null", imports );
            }

            return new TypeScriptTypeDefinition( tsType, imports );
        }

        /// <summary>
        /// Gets the class name to use for the type.
        /// </summary>
        /// <param name="type">The type whose name is to be generated.</param>
        /// <returns>A string that represents the class name.</returns>
        private static string GetClassNameForType( Type type )
        {
            if ( type.IsGenericType )
            {
                var genericTypes = type.GetGenericArguments().Select( t => t.Name ).ToList();

                return $"{type.Name.Split( '`' )[0]}<{string.Join( ", ", genericTypes )}>";
            }

            return type.Name;
        }

        /// <summary>
        /// Gets the summary plain text documentation for the type.
        /// </summary>
        /// <param name="type">The type to retrieve documentation for.</param>
        /// <returns>A string containing the summary plain text or <c>null</c>.</returns>
        [ExcludeFromCodeCoverage]
        private string GetDocumentationSummary( Type type )
        {
            return DocumentationProvider?.GetTypeComments( type )?.Summary?.PlainText;
        }

        /// <summary>
        /// Gets the summary plain text documentation for the member.
        /// </summary>
        /// <param name="memberInfo">The member to retrieve documentation for.</param>
        /// <returns>A string containing the summary plain text or <c>null</c>.</returns>
        [ExcludeFromCodeCoverage]
        private string GetDocumentationSummary( MemberInfo memberInfo )
        {
            return DocumentationProvider?.GetMemberComments( memberInfo )?.Summary?.PlainText;
        }

        #endregion
    }
}
