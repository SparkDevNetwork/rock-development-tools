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
    }
}
