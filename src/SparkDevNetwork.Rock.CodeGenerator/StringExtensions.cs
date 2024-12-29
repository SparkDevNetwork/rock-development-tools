using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SparkDevNetwork.Rock.CodeGenerator
{
    /// <summary>
    /// Extension methods for the <see cref="string"/> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <inheritdoc cref="string.IsNullOrWhiteSpace(string)"/>
        [ExcludeFromCodeCoverage]
        public static bool IsNullOrWhiteSpace( this string str )
        {
            return string.IsNullOrWhiteSpace( str );
        }

        /// <summary>
        /// Indicates whether a specified string is not null, empty, or
        /// consists only of white-space characters.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>true if the string is not null, not empty, or consists of any non white-space characters.</returns>
        [ExcludeFromCodeCoverage]
        public static bool IsNotNullOrWhiteSpace( this string str )
        {
            return !string.IsNullOrWhiteSpace( str );
        }

        /// <summary>
        /// Splits a Camel or Pascal cased identifier into separate words.
        /// </summary>
        /// <param name="str">The identifier.</param>
        /// <returns></returns>
        public static string SplitCase( this string str )
        {
            if ( str == null )
            {
                return null;
            }

            return Regex.Replace( Regex.Replace( str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2" ), @"(\p{Ll})(\P{Ll})", "$1 $2" );
        }

        /// <summary>
        /// Returns a substring of a string. Uses an empty string for any part
        /// that doesn't exist and will return a partial substring if the
        /// string isn't long enough for the requested length (the built-in
        /// method would throw an exception in these cases).
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="startIndex">The 0-based starting position.</param>
        /// <returns></returns>
        public static string SubstringSafe( this string str, int startIndex )
        {
            if ( str == null )
            {
                return string.Empty;
            }

            return str.SubstringSafe( startIndex, Math.Max( str.Length - startIndex, 0 ) );
        }

        /// <summary>
        /// Returns a substring of a string. Uses an empty string for any part
        /// that doesn't exist and will return a partial substring if the
        /// string isn't long enough for the requested length (the built-in
        /// method would throw an exception in these cases).
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="startIndex">The 0-based starting position.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <returns></returns>
        public static string SubstringSafe( this string str, int startIndex, int maxLength )
        {
            if ( str == null || maxLength < 0 || startIndex < 0 || startIndex > str.Length )
            {
                return string.Empty;
            }

            return str.Substring( startIndex, Math.Min( maxLength, str.Length - startIndex ) );
        }

        /// <summary>
        /// Convert a string into camelCase.
        /// </summary>
        /// <remarks>Originally from https://github.com/JamesNK/Newtonsoft.Json/blob/01e1759cac40d8154e47ed0e11c12a9d42d2d0ff/Src/Newtonsoft.Json/Utilities/StringUtils.cs#L155</remarks>
        /// <param name="value">The string to be converted.</param>
        /// <returns>A string in camel case.</returns>
        public static string ToCamelCase( this string value )
        {
            if ( string.IsNullOrEmpty( value ) || !char.IsUpper( value[0] ) )
            {
                return value;
            }

            var chars = value.ToCharArray();

            for ( int i = 0; i < chars.Length; i++ )
            {
                if ( i == 1 && !char.IsUpper( chars[i] ) )
                {
                    break;
                }

                var hasNext = i + 1 < chars.Length;

                if ( i > 0 && hasNext && !char.IsUpper( chars[i + 1] ) )
                {
                    // if the next character is a space, which is not considered uppercase 
                    // (otherwise we wouldn't be here...)
                    // we want to ensure that the following:
                    // 'FOO bar' is rewritten as 'foo bar', and not as 'foO bar'
                    // The code was written in such a way that the first word in uppercase
                    // ends when if finds an uppercase letter followed by a lowercase letter.
                    // now a ' ' (space, (char)32) is considered not upper
                    // but in that case we still want our current character to become lowercase
                    if ( char.IsSeparator( chars[i + 1] ) )
                    {
                        chars[i] = char.ToLower( chars[i] );
                    }

                    break;
                }

                chars[i] = char.ToLower( chars[i] );
            }

            return new string( chars );
        }

        /// <summary>
        /// Calls the ToString method on the object or an empty string
        /// if it was <c>null</c>.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>A string.</returns>
        public static string ToStringSafe( this object value )
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}
