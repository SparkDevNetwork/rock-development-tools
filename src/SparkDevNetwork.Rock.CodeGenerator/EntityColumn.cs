using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Various bits of logic for dealing with columns that are to be passed
    /// to the Lava templates for processing.
    /// </summary>
    public class EntityColumn
    {
        #region Fields

        /// <summary>
        /// The simple primitive types.
        /// </summary>
        private static readonly string[] _primitiveTypes = new[]
        {
            typeof( bool ).FullName,
            typeof( bool? ).FullName,
            typeof( int ).FullName,
            typeof( int? ).FullName,
            typeof( long ).FullName,
            typeof( long? ).FullName,
            typeof( decimal ).FullName,
            typeof( decimal? ).FullName,
            typeof( double ).FullName,
            typeof( double? ).FullName,
            typeof( string ).FullName,
            typeof( Guid ).FullName,
            typeof( Guid? ).FullName
        };

        /// <summary>
        /// The numeric types.
        /// </summary>
        private static readonly string[] _numericTypes = new[]
        {
            typeof( int ).FullName,
            typeof( int? ).FullName,
            typeof( long ).FullName,
            typeof( long? ).FullName,
            typeof( decimal ).FullName,
            typeof( decimal? ).FullName,
            typeof( double ).FullName,
            typeof( double? ).FullName
        };

        /// <summary>
        /// The simple date types.
        /// </summary>
        private static readonly string[] _dateTypes = new[]
        {
            typeof( DateTime ).FullName,
            typeof( DateTime? ).FullName,
            typeof( DateTimeOffset ).FullName,
            typeof( DateTimeOffset? ).FullName
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets the property information.
        /// </summary>
        /// <value>The property information.</value>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <value>The type of the property.</value>
        public Type PropertyType => PropertyInfo.PropertyType;

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string Name => PropertyInfo.Name;

        /// <summary>
        /// Gets the friendly name of the column.
        /// </summary>
        /// <value>The friendly name of the column.</value>
        public string FriendlyName
        {
            get
            {
                // Strip "Value" off end of defined value navigations.
                if ( PropertyType.FullName == "Rock.Model.DefinedValue" && Name.EndsWith( "Value" ) )
                {
                    return Name.Substring( 0, Name.Length - 5 );
                }

                // Strip "PersonAlias" off end of person alias navigations.
                if ( PropertyType.FullName == "Rock.Model.PersonAlias" && Name.EndsWith( "PersonAlias" ) )
                {
                    return Name.Substring( 0, Name.Length - 11 );
                }

                // Strip "Alias" off end of person alias navigations.
                if ( PropertyType.FullName == "Rock.Model.PersonAlias" && Name.EndsWith( "Alias" ) )
                {
                    return Name.Substring( 0, Name.Length - 5 );
                }

                return Name;
            }
        }

        /// <summary>
        /// Gets the code to add the field to the builder.
        /// </summary>
        /// <value>The code to add the field to the builder.</value>
        public string AddFieldCode => GetAddFieldCode();

        /// <summary>
        /// Gets the column template code.
        /// </summary>
        /// <value>The column template code.</value>
        public string TemplateCode => GetTemplateCode();

        /// <summary>
        /// Gets the name of the imports from the Grid package.
        /// </summary>
        /// <value>The name of the imports from the Grid package.</value>
        public IEnumerable<string> GridImports => GetGridImports();

        /// <summary>
        /// Gets a value indicating whether this property is an entity.
        /// </summary>
        /// <value><c>true</c> if this property is an entity; otherwise, <c>false</c>.</value>
        public bool IsEntity => PropertyType.ImplementsInterface( "Rock.Data.IEntity" );

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityColumn"/> class.
        /// </summary>
        /// <param name="propertyInfo">The property to be represented by this instance.</param>
        public EntityColumn( PropertyInfo propertyInfo )
        {
            PropertyInfo = propertyInfo;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the C# code that will handle adding the field to the builder.
        /// </summary>
        /// <returns>A string that represents the C# code.</returns>
        public string GetAddFieldCode()
        {
            // Check if date type.
            if ( _dateTypes.Contains( PropertyType.FullName ) )
            {
                return $".AddDateTimeField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name} )";
            }

            // Check if Person.
            if ( PropertyType.FullName == "Rock.Model.PersonAlias" )
            {
                return $".AddPersonField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name}?.Person )";
            }

            // Check if generic entity.
            if ( IsEntity )
            {
                if ( PropertyType.GetProperty( "Name" ) != null )
                {
                    return $".AddTextField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name}?.Name )";
                }
                else if ( PropertyType.GetProperty( "Title" ) != null )
                {
                    return $".AddTextField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name}?.Title )";
                }
                else if ( PropertyType.FullName == "Rock.Model.DefinedValue" )
                {
                    return $".AddTextField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name}?.Value )";
                }
                else
                {
                    return $".AddTextField( \"{FriendlyName.ToCamelCase()}\", a => throw new NotSupportedException() )";
                }
            }

            // Check for string type.
            if ( PropertyType == typeof( string ) )
            {
                return $".AddTextField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name} )";
            }

            // Check if it is a simple primitive type.
            if ( _primitiveTypes.Contains( PropertyType.FullName ) )
            {
                return $".AddField( \"{FriendlyName.ToCamelCase()}\", a => a.{Name} )";
            }

            return $".AddField( \"{FriendlyName.ToCamelCase()}, a => throw new NotSupportedException() )";
        }

        /// <summary>
        /// Gets the C# code that will handle the code for the column template
        /// definition in the Obsidian file.
        /// </summary>
        /// <returns>A string that represents the C# code.</returns>
        public string GetTemplateCode()
        {
            // Check for string types.
            if ( PropertyType.FullName == typeof( string ).FullName || PropertyType.FullName == typeof( Guid ).FullName || PropertyType.FullName == typeof( Guid? ).FullName )
            {
                return $@"
        <TextColumn name=""{FriendlyName.ToCamelCase()}""
                    title=""{FriendlyName.SplitCase()}""
                    field=""{FriendlyName.ToCamelCase()}""
                    :filter=""textValueFilter""
                    visiblePriority=""xs"" />".Trim();
            }

            // Check for numeric types.
            if ( _numericTypes.Contains( PropertyType.FullName ) )
            {
                return $@"
        <NumberColumn name=""{FriendlyName.ToCamelCase()}""
                       title=""{FriendlyName.SplitCase()}""
                       field=""{FriendlyName.ToCamelCase()}""
                       :filter=""numberValueFilter""
                       visiblePriority=""xs"" />".Trim();
            }

            // Check for boolean types.
            if ( PropertyType.FullName == typeof( bool ).FullName || PropertyType.FullName == typeof( bool? ).FullName )
            {
                return $@"
        <BooleanColumn name=""{FriendlyName.ToCamelCase()}""
                       title=""{FriendlyName.SplitCase()}""
                       field=""{FriendlyName.ToCamelCase()}""
                       visiblePriority=""xs"" />".Trim();
            }

            // Check for date types.
            if ( _dateTypes.Contains( PropertyType.FullName ) )
            {
                return $@"
        <DateColumn name=""{FriendlyName.ToCamelCase()}""
                    title=""{FriendlyName.SplitCase()}""
                    field=""{FriendlyName.ToCamelCase()}""
                    :filter=""dateValueFilter""
                    visiblePriority=""xs"" />".Trim();
            }

            // Check for Person types.
            if ( PropertyType.FullName == "Rock.Model.PersonAlias" )
            {
                return $@"
        <PersonColumn name=""{FriendlyName.ToCamelCase()}""
                      title=""{FriendlyName.SplitCase()}""
                      field=""{FriendlyName.ToCamelCase()}""
                      :filter=""pickExistingValueFilter""
                      visiblePriority=""xs"" />".Trim();
            }

            // Check for generic entity types.
            if ( IsEntity )
            {
                return $@"
        <TextColumn name=""{FriendlyName.ToCamelCase()}""
                    title=""{FriendlyName.SplitCase()}""
                    field=""{FriendlyName.ToCamelCase()}""
                    :filter=""textValueFilter""
                    visiblePriority=""xs"" />".Trim();
            }

            return $@"
        <Column name=""{FriendlyName.ToCamelCase()}""
                title=""{FriendlyName.SplitCase()}""
                visiblePriority=""xs"">
            <template #format=""{{ row }}"">
                {{{{ row.{FriendlyName.ToCamelCase()} }}}}
            </template>
        </Column>
".Trim();
        }

        /// <summary>
        /// Gets the template column name.
        /// </summary>
        /// <returns>A collection of strings that contains the import names.</returns>
        private IEnumerable<string> GetGridImports()
        {
            // Check for string types.
            if ( PropertyType.FullName == typeof( string ).FullName || PropertyType.FullName == typeof( Guid ).FullName || PropertyType.FullName == typeof( Guid? ).FullName )
            {
                return new[] { "TextColumn", "textValueFilter" };
            }

            // Check for numeric types.
            if ( _numericTypes.Contains( PropertyType.FullName ) )
            {
                return new[] { "NumberColumn", "numberValueFilter" };
            }

            // Check for boolean types.
            if ( PropertyType.FullName == typeof( bool ).FullName || PropertyType.FullName == typeof( bool? ).FullName )
            {
                return new[] { "BooleanColumn" };
            }

            // Check for date types.
            if ( _dateTypes.Contains( PropertyType.FullName ) )
            {
                return new[] { "DateColumn", "dateValueFilter" };
            }

            // Check for Person types.
            if ( PropertyType.FullName == "Rock.Model.PersonAlias" )
            {
                return new[] { "PersonColumn", "pickExistingValueFilter" };
            }

            // Check for generic entity types.
            if ( IsEntity )
            {
                return new[] { "TextColumn", "textValueFilter" };
            }

            return new[] { "Column" };
        }

        /// <summary>
        /// Determines whether the type is one that is supported for normal
        /// code generation operations.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns><c>true</c> if the type is supported; otherwise, <c>false</c>.</returns>
        public static bool IsSupportedPropertyType( Type type )
        {
            // If the type is one of the few supported entity types or one of
            // the known primitive types then it is considered supported.
            if ( type.ImplementsInterface( "Rock.Data.IEntity" ) )
            {
                return true;
            }
            else if ( _primitiveTypes.Contains( type.FullName ) )
            {
                return true;
            }
            else if ( _dateTypes.Contains( type.FullName ) )
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
