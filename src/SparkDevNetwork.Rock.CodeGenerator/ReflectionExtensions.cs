using System;
using System.Linq;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Extension methods for various reflection types.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets the custom attribute data for a member that matches the
        /// specified full CLR name of the attribute. If multiple attributes
        /// are found then only the first one is returned.
        /// </summary>
        /// <param name="member">The member to be inspected.</param>
        /// <param name="fullName">The full CLR name of the attribute.</param>
        /// <returns>An instance of <see cref="CustomAttributeData"/> that represents the attribute.</returns>
        public static CustomAttributeData GetCustomAttributeData( this MemberInfo member, string fullName )
        {
            return member.GetCustomAttributesData().FirstOrDefault( a => a.AttributeType.FullName == fullName );
        }

        /// <summary>
        /// Determines if a type implements an interface with the specified
        /// full CLR interface name.
        /// </summary>
        /// <param name="type">The type to be inspected.</param>
        /// <param name="fullName">The full CLR name of the interface.</param>
        /// <returns><c>true</c> if the type implements the interface; otherwise <c>false</c>.</returns>
        public static bool ImplementsInterface( this Type type, string fullName )
        {
            foreach ( var i in type.GetInterfaces() )
            {
                if ( i.IsGenericType && i.GetGenericTypeDefinition().FullName == fullName )
                {
                    return true;
                }
                else if ( !i.IsGenericType && i.FullName == fullName )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a friendly name for the type. This turns generic type names into
        /// something closer to C# syntax.
        /// </summary>
        /// <param name="type">The type whose name should be fetched.</param>
        /// <returns>A string that represents the friendly name.</returns>
        public static string GetFriendlyName( this Type type )
        {
            if ( type.IsGenericType )
            {
                var namePrefix = type.Name.Split( new[] { '`' }, StringSplitOptions.RemoveEmptyEntries )[0];
                var genericParameters = type.GetGenericArguments().Select( GetFriendlyName );

                return $"{namePrefix}<{string.Join( ", ", genericParameters)}>";
            }

            return type.Name;
        }

        /// <summary>
        /// Gets the C# property declaration for the given type.
        /// </summary>
        /// <param name="type">The type that needs to be declared.</param>
        /// <returns>A <see cref="PropertyDeclaration"/> instance that represents the declaration.</returns>
        internal static PropertyDeclaration GetCSharpPropertyDeclaration( this Type type )
        {
            if ( type.FullName == typeof( bool ).FullName )
            {
                return new PropertyDeclaration( "bool" );
            }
            else if ( type.FullName == typeof( bool? ).FullName )
            {
                return new PropertyDeclaration( "bool?" );
            }
            else if ( type.FullName == typeof( int ).FullName )
            {
                return new PropertyDeclaration( "int" );
            }
            else if ( type.FullName == typeof( int? ).FullName )
            {
                return new PropertyDeclaration( "int?" );
            }
            else if ( type.FullName == typeof( long ).FullName )
            {
                return new PropertyDeclaration( "long" );
            }
            else if ( type.FullName == typeof( long? ).FullName )
            {
                return new PropertyDeclaration( "long?" );
            }
            else if ( type.FullName == typeof( decimal ).FullName )
            {
                return new PropertyDeclaration( "decimal" );
            }
            else if ( type.FullName == typeof( decimal? ).FullName )
            {
                return new PropertyDeclaration( "decimal?" );
            }
            else if ( type.FullName == typeof( double ).FullName )
            {
                return new PropertyDeclaration( "double" );
            }
            else if ( type.FullName == typeof( double? ).FullName )
            {
                return new PropertyDeclaration( "double?" );
            }
            else if ( type.FullName == typeof( string ).FullName )
            {
                return new PropertyDeclaration( "string" );
            }
            else if ( type.FullName == typeof( Guid ).FullName )
            {
                return new PropertyDeclaration( "Guid", new[] { "System" } );
            }
            else if ( type.FullName == typeof( Guid? ).FullName )
            {
                return new PropertyDeclaration( "Guid?", new[] { "System" } );
            }
            else if ( type.FullName == typeof( DateTime ).FullName )
            {
                return new PropertyDeclaration( "DateTime", new[] { "System" } );
            }
            else if ( type.FullName == typeof( DateTime? ).FullName )
            {
                return new PropertyDeclaration( "DateTime?", new[] { "System" } );
            }
            else if ( type.FullName == typeof( DateTimeOffset ).FullName )
            {
                return new PropertyDeclaration( "DateTimeOffset", new[] { "System" } );
            }
            else if ( type.FullName == typeof( DateTimeOffset? ).FullName )
            {
                return new PropertyDeclaration( "DateTimeOffset?", new[] { "System" } );
            }
            else if ( type.IsEnum && type.Namespace.StartsWith( "Rock.Enums" ) )
            {
                return new PropertyDeclaration( type.Name, new[] { type.Namespace } );
            }
            else if ( type.IsEnum && type.GetCustomAttributeData( "Rock.Enums.EnumDomainAttribute" ) != null )
            {
                return new PropertyDeclaration( type.Name, new[] { type.Namespace } );
            }
            else
            {
                throw new Exception( $"Unable to convert {type.GetFriendlyName()} to CSharp declaration." );
            }
        }

        /// <summary>
        /// Determines if the type is a Rock entity type, that is if it
        /// implements <c>Rock.Data.IEntity</c>.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns><c>true</c> if the type is a Rock entity type.</returns>
        public static bool IsRockEntity( this Type type )
        {
            return type.ImplementsInterface( "Rock.Data.IEntity" );
        }
    }
}
