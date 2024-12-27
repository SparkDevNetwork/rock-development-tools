using System;
using System.Collections.Generic;
using System.Linq;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Extension methods for the <see cref="IEnumerable<T>"/>  type.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Takes the last n items from a List.
        /// </summary>
        /// <remarks>
        /// http://stackoverflow.com/questions/3453274/using-linq-to-get-the-last-n-elements-of-a-collection
        /// </remarks>
        /// <typeparam name="T">The type of element in the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="count">The maximum number of elements to take from the end of the list.</param>
        /// <returns>A new enumerable that contains the last <paramref name="count"/> items.</returns>
        public static IEnumerable<T> TakeLast<T>( this IEnumerable<T> source, int count )
        {
            return source.Skip( Math.Max( 0, source.Count() - count ) );
        }
    }
}
