using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Various bits of logic for dealing with properties that are to be passed
    /// to the Lava templates for processing.
    /// </summary>
    public class EntityProperty
    {
        #region Fields

        /// <summary>
        /// The types that should be handled by simple assignment.
        /// </summary>
        private static readonly string[] _assignmentTypes = new[]
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
            typeof( Guid? ).FullName,
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
        /// Gets the convert to bag code.
        /// </summary>
        /// <value>The convert to bag code.</value>
        public string ConvertToBagCode => GetConvertToBagCode( false );

        /// <summary>
        /// Gets the convert from bag code.
        /// </summary>
        /// <value>The convert from bag code.</value>
        public string ConvertFromBagCode => GetConvertFromBagCode( false );

        /// <summary>
        /// Gets a value indicating whether this property is an entity.
        /// </summary>
        /// <value><c>true</c> if this property is an entity; otherwise, <c>false</c>.</value>
        public bool IsEntity => PropertyType.IsRockEntity();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityProperty"/> class.
        /// </summary>
        /// <param name="propertyInfo">The property to be represented by this instance.</param>
        public EntityProperty( PropertyInfo propertyInfo )
        {
            PropertyInfo = propertyInfo;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the C# code that will handle converting the value from an
        /// entity value into the bag value.
        /// </summary>
        /// <param name="throwOnError">If set to <c>true</c> then an exception will be thrown if there is an error.</param>
        /// <returns>A string that represents the C# code.</returns>
        public string GetConvertToBagCode( bool throwOnError = true )
        {
            // Check if it is a simple assignment type.
            if ( _assignmentTypes.Contains( PropertyType.FullName ) )
            {
                return Name;
            }

            // If the type is an IEntity, then use the standard conversion.
            if ( PropertyType.IsRockEntity() )
            {
                return $"{Name}.ToListItemBag()";
            }

            // If the type is a collection of IEntity types, then use the
            // standard conversion.
            if ( PropertyType.IsGenericType && PropertyType.GenericTypeArguments.Length == 1 )
            {
                var genericArg = PropertyType.GenericTypeArguments[0];
                var collectionType = typeof( ICollection<> ).MakeGenericType( genericArg );

                if ( PropertyType.ImplementsInterface( collectionType.FullName ) )
                {
                    return $"{Name}.ToListItemBagList()";
                }
            }

            return throwOnError
                ? throw new Exception( $"Unknown property type '{PropertyType.GetFriendlyName()}' for conversion to bag." )
                : $"/* TODO: Unknown property type '{PropertyType.GetFriendlyName()}' for conversion to bag. */";
        }

        /// <summary>
        /// Gets the C# code that will handle converting the value from a bag
        /// value into the entity value.
        /// </summary>
        /// <param name="throwOnError">If set to <c>true</c> then an exception will be thrown if there is an error.</param>
        /// <returns>A string that represents the C# code.</returns>
        public string GetConvertFromBagCode( bool throwOnError = true )
        {
            // Check if it is a simple assignment type.
            if ( _assignmentTypes.Contains( PropertyType.FullName ) )
            {
                return Name;
            }

            // If the type is an IEntity, then use the standard conversion.
            if ( PropertyType.IsRockEntity() )
            {
                var idProperty = PropertyInfo.DeclaringType.GetProperty( $"{PropertyType.Name}Id" );

                // If the id property is not nullable, get the required integer value.
                if ( idProperty != null && idProperty.PropertyType.FullName == typeof( int ).FullName )
                {
                    return $"{Name}.GetEntityId<{PropertyType.GetFriendlyName()}>( RockContext ).Value";
                }

                return $"{Name}.GetEntityId<{PropertyType.GetFriendlyName()}>( RockContext )";
            }

            // We don't know how to handle it, so either throw an error or put
            // an error in the source code.
            return throwOnError
                ? throw new Exception( $"Unknown property type '{PropertyType.GetFriendlyName()}' for conversion to bag." )
                : $"/* TODO: Unknown property type '{PropertyType.GetFriendlyName()}' for conversion to bag. */";
        }

        /// <summary>
        /// Determines whether the type is one that is supported for normal
        /// code generation operations.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns><c>true</c> if the type is supported; otherwise, <c>false</c>.</returns>
        public static bool IsSupportedPropertyType( Type type )
        {
            // If the type is a collection of supported types then it is supported.
            if ( type.IsGenericType && type.GenericTypeArguments.Length == 1 )
            {
                var genericArg = type.GenericTypeArguments[0];
                var collectionType = typeof( ICollection<> ).MakeGenericType( genericArg );

                if ( type.ImplementsInterface( collectionType.FullName ) )
                {
                    return true;
                }
            }

            // If the type is an entity type or one of the known primitive
            // types then it is considered supported.
            if ( type.IsRockEntity() )
            {
                return true;
            }
            else if ( type.FullName == typeof( bool ).FullName || type.FullName == typeof( bool? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( int ).FullName || type.FullName == typeof( int? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( long ).FullName || type.FullName == typeof( long? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( decimal ).FullName || type.FullName == typeof( decimal? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( double ).FullName || type.FullName == typeof( double? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( Guid ).FullName || type.FullName == typeof( Guid? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( string ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( DateTime ).FullName || type.FullName == typeof( DateTime? ).FullName )
            {
                return true;
            }
            else if ( type.FullName == typeof( DateTimeOffset ).FullName || type.FullName == typeof( DateTimeOffset? ).FullName )
            {
                return true;
            }
            else if ( type.IsEnum && type.Namespace.StartsWith( "Rock.Enums" ) )
            {
                return true;
            }
            else if ( type.IsEnum && type.GetCustomAttributeData( "Rock.Enums.EnumDomainAttribute" ) != null )
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
