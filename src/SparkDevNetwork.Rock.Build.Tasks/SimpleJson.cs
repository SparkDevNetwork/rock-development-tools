using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace SparkDevNetwork.Rock.Build.Tasks
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// </summary>
    /// <remarks>
    /// This was originally taken from https://github.com/facebook-csharp-sdk/simple-json/blob/master/src/SimpleJson/SimpleJson.cs
    /// and then heavily modified to remove everything we didn't need.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    static class SimpleJson
    {
        private const int TOKEN_NONE = 0;
        private const int TOKEN_CURLY_OPEN = 1;
        private const int TOKEN_CURLY_CLOSE = 2;
        private const int TOKEN_SQUARED_OPEN = 3;
        private const int TOKEN_SQUARED_CLOSE = 4;
        private const int TOKEN_COLON = 5;
        private const int TOKEN_COMMA = 6;
        private const int TOKEN_STRING = 7;
        private const int TOKEN_NUMBER = 8;
        private const int TOKEN_TRUE = 9;
        private const int TOKEN_FALSE = 10;
        private const int TOKEN_NULL = 11;
        private const int BUILDER_CAPACITY = 2000;

        private static readonly char[] EscapeTable;
        private static readonly char[] EscapeCharacters = new char[] { '"', '\\', '\b', '\f', '\n', '\r', '\t' };

        static SimpleJson()
        {
            EscapeTable = new char[93];
            EscapeTable['"'] = '"';
            EscapeTable['\\'] = '\\';
            EscapeTable['\b'] = 'b';
            EscapeTable['\f'] = 'f';
            EscapeTable['\n'] = 'n';
            EscapeTable['\r'] = 'r';
            EscapeTable['\t'] = 't';
        }

        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An IList&lt;object>, a IDictionary&lt;string,object>, a double, a string, null, true, or false</returns>
        public static object DeserializeObject( string json )
        {
            if ( TryDeserializeObject( json, out var obj ) )
            {
                return obj;
            }

            throw new SerializationException( "Invalid JSON string" );
        }

        /// <summary>
        /// Try parsing the json string into a value.
        /// </summary>
        /// <param name="json">
        /// A JSON string.
        /// </param>
        /// <param name="obj">
        /// The object.
        /// </param>
        /// <returns>
        /// Returns true if successfull otherwise false.
        /// </returns>
        public static bool TryDeserializeObject( string json, out object obj )
        {
            bool success = true;
            if ( json != null )
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                obj = ParseValue( charArray, ref index, ref success );
            }
            else
                obj = null;

            return success;
        }

        static IDictionary<string, object> ParseObject( char[] json, ref int index, ref bool success )
        {
            IDictionary<string, object> table = new Dictionary<string, object>();
            int token;

            // {
            NextToken( json, ref index );

            bool done = false;
            while ( !done )
            {
                token = LookAhead( json, index );
                if ( token == TOKEN_NONE )
                {
                    success = false;
                    return null;
                }
                else if ( token == TOKEN_COMMA )
                    NextToken( json, ref index );
                else if ( token == TOKEN_CURLY_CLOSE )
                {
                    NextToken( json, ref index );
                    return table;
                }
                else
                {
                    // name
                    string name = ParseString( json, ref index, ref success );
                    if ( !success )
                    {
                        success = false;
                        return null;
                    }
                    // :
                    token = NextToken( json, ref index );
                    if ( token != TOKEN_COLON )
                    {
                        success = false;
                        return null;
                    }
                    // value
                    object value = ParseValue( json, ref index, ref success );
                    if ( !success )
                    {
                        success = false;
                        return null;
                    }
                    table[name] = value;
                }
            }
            return table;
        }

        static List<object> ParseArray( char[] json, ref int index, ref bool success )
        {
            var array = new List<object>();

            // [
            NextToken( json, ref index );

            bool done = false;
            while ( !done )
            {
                int token = LookAhead( json, index );
                if ( token == TOKEN_NONE )
                {
                    success = false;
                    return null;
                }
                else if ( token == TOKEN_COMMA )
                    NextToken( json, ref index );
                else if ( token == TOKEN_SQUARED_CLOSE )
                {
                    NextToken( json, ref index );
                    break;
                }
                else
                {
                    object value = ParseValue( json, ref index, ref success );
                    if ( !success )
                        return null;
                    array.Add( value );
                }
            }
            return array;
        }

        static object ParseValue( char[] json, ref int index, ref bool success )
        {
            switch ( LookAhead( json, index ) )
            {
                case TOKEN_STRING:
                    return ParseString( json, ref index, ref success );
                case TOKEN_NUMBER:
                    return ParseNumber( json, ref index, ref success );
                case TOKEN_CURLY_OPEN:
                    return ParseObject( json, ref index, ref success );
                case TOKEN_SQUARED_OPEN:
                    return ParseArray( json, ref index, ref success );
                case TOKEN_TRUE:
                    NextToken( json, ref index );
                    return true;
                case TOKEN_FALSE:
                    NextToken( json, ref index );
                    return false;
                case TOKEN_NULL:
                    NextToken( json, ref index );
                    return null;
                case TOKEN_NONE:
                    break;
            }
            success = false;
            return null;
        }

        static string ParseString( char[] json, ref int index, ref bool success )
        {
            StringBuilder s = new StringBuilder( BUILDER_CAPACITY );
            char c;

            EatWhitespace( json, ref index );

            // "
            c = json[index++];
            bool complete = false;
            while ( !complete )
            {
                if ( index == json.Length )
                    break;

                c = json[index++];
                if ( c == '"' )
                {
                    complete = true;
                    break;
                }
                else if ( c == '\\' )
                {
                    if ( index == json.Length )
                        break;
                    c = json[index++];
                    if ( c == '"' )
                        s.Append( '"' );
                    else if ( c == '\\' )
                        s.Append( '\\' );
                    else if ( c == '/' )
                        s.Append( '/' );
                    else if ( c == 'b' )
                        s.Append( '\b' );
                    else if ( c == 'f' )
                        s.Append( '\f' );
                    else if ( c == 'n' )
                        s.Append( '\n' );
                    else if ( c == 'r' )
                        s.Append( '\r' );
                    else if ( c == 't' )
                        s.Append( '\t' );
                    else if ( c == 'u' )
                    {
                        int remainingLength = json.Length - index;
                        if ( remainingLength >= 4 )
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if ( !( success = UInt32.TryParse( new string( json, index, 4 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint ) ) )
                                return "";

                            // convert the integer codepoint to a unicode char and add to string
                            if ( 0xD800 <= codePoint && codePoint <= 0xDBFF )  // if high surrogate
                            {
                                index += 4; // skip 4 chars
                                remainingLength = json.Length - index;
                                if ( remainingLength >= 6 )
                                {
                                    uint lowCodePoint;
                                    if ( new string( json, index, 2 ) == "\\u" && UInt32.TryParse( new string( json, index + 2, 4 ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lowCodePoint ) )
                                    {
                                        if ( 0xDC00 <= lowCodePoint && lowCodePoint <= 0xDFFF )    // if low surrogate
                                        {
                                            s.Append( ( char ) codePoint );
                                            s.Append( ( char ) lowCodePoint );
                                            index += 6; // skip 6 chars
                                            continue;
                                        }
                                    }
                                }
                                success = false;    // invalid surrogate pair
                                return "";
                            }
                            s.Append( ConvertFromUtf32( ( int ) codePoint ) );
                            // skip 4 chars
                            index += 4;
                        }
                        else
                            break;
                    }
                }
                else
                    s.Append( c );
            }
            if ( !complete )
            {
                success = false;
                return null;
            }
            return s.ToString();
        }

        private static string ConvertFromUtf32( int utf32 )
        {
            // http://www.java2s.com/Open-Source/CSharp/2.6.4-mono-.net-core/System/System/Char.cs.htm
            if ( utf32 < 0 || utf32 > 0x10FFFF )
                throw new ArgumentOutOfRangeException( "utf32", "The argument must be from 0 to 0x10FFFF." );
            if ( 0xD800 <= utf32 && utf32 <= 0xDFFF )
                throw new ArgumentOutOfRangeException( "utf32", "The argument must not be in surrogate pair range." );
            if ( utf32 < 0x10000 )
                return new string( ( char ) utf32, 1 );
            utf32 -= 0x10000;
            return new string( new char[] { ( char ) ( ( utf32 >> 10 ) + 0xD800 ), ( char ) ( utf32 % 0x0400 + 0xDC00 ) } );
        }

        static object ParseNumber( char[] json, ref int index, ref bool success )
        {
            EatWhitespace( json, ref index );
            int lastIndex = GetLastIndexOfNumber( json, index );
            int charLength = ( lastIndex - index ) + 1;
            object returnNumber;
            string str = new string( json, index, charLength );
            if ( str.IndexOf( ".", StringComparison.OrdinalIgnoreCase ) != -1 || str.IndexOf( "e", StringComparison.OrdinalIgnoreCase ) != -1 )
            {
                double number;
                success = double.TryParse( new string( json, index, charLength ), NumberStyles.Any, CultureInfo.InvariantCulture, out number );
                returnNumber = number;
            }
            else
            {
                long number;
                success = long.TryParse( new string( json, index, charLength ), NumberStyles.Any, CultureInfo.InvariantCulture, out number );
                returnNumber = number;
            }
            index = lastIndex + 1;
            return returnNumber;
        }

        static int GetLastIndexOfNumber( char[] json, int index )
        {
            int lastIndex;
            for ( lastIndex = index; lastIndex < json.Length; lastIndex++ )
                if ( "0123456789+-.eE".IndexOf( json[lastIndex] ) == -1 ) break;
            return lastIndex - 1;
        }

        static void EatWhitespace( char[] json, ref int index )
        {
            for ( ; index < json.Length; index++ )
                if ( " \t\n\r\b\f".IndexOf( json[index] ) == -1 ) break;
        }

        static int LookAhead( char[] json, int index )
        {
            int saveIndex = index;
            return NextToken( json, ref saveIndex );
        }

        static int NextToken( char[] json, ref int index )
        {
            EatWhitespace( json, ref index );
            if ( index == json.Length )
                return TOKEN_NONE;
            char c = json[index];
            index++;
            switch ( c )
            {
                case '{':
                    return TOKEN_CURLY_OPEN;
                case '}':
                    return TOKEN_CURLY_CLOSE;
                case '[':
                    return TOKEN_SQUARED_OPEN;
                case ']':
                    return TOKEN_SQUARED_CLOSE;
                case ',':
                    return TOKEN_COMMA;
                case '"':
                    return TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return TOKEN_NUMBER;
                case ':':
                    return TOKEN_COLON;
            }
            index--;
            int remainingLength = json.Length - index;
            // false
            if ( remainingLength >= 5 )
            {
                if ( json[index] == 'f' && json[index + 1] == 'a' && json[index + 2] == 'l' && json[index + 3] == 's' && json[index + 4] == 'e' )
                {
                    index += 5;
                    return TOKEN_FALSE;
                }
            }
            // true
            if ( remainingLength >= 4 )
            {
                if ( json[index] == 't' && json[index + 1] == 'r' && json[index + 2] == 'u' && json[index + 3] == 'e' )
                {
                    index += 4;
                    return TOKEN_TRUE;
                }
            }
            // null
            if ( remainingLength >= 4 )
            {
                if ( json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l' )
                {
                    index += 4;
                    return TOKEN_NULL;
                }
            }
            return TOKEN_NONE;
        }
    }
}
