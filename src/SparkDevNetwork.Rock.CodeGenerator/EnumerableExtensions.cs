using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Extension methods for the <see cref="IEnumerable<T>"/>  type.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Takes the last n items from a List.
        /// http://stackoverflow.com/questions/3453274/using-linq-to-get-the-last-n-elements-of-a-collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="N">The n.</param>
        /// <returns></returns>
        public static IEnumerable<T> TakeLast<T>( this IEnumerable<T> source, int N )
        {
            return source.Skip( Math.Max( 0, source.Count() - N ) );
        }
    }

    public static class ReflectionExtensions
    {
        public static CustomAttributeData GetCustomAttributeData( this MemberInfo member, string fullName )
        {
            return member.GetCustomAttributesData().FirstOrDefault( a => a.AttributeType.FullName == fullName );
        }

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
