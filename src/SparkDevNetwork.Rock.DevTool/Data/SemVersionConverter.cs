using System.Text.Json;
using System.Text.Json.Serialization;

using Semver;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// Handles serialization and deserialization of <see cref="SemVersion"/> objects
/// when reading and writin JSON files.
/// </summary>
class SemVersionConverter : JsonConverter<SemVersion>
{
    /// <summary>
    /// An empty version that can be used for comparison.
    /// </summary>
    private static readonly SemVersion EmptyVersion = new( 0, 0, 0 );

    /// <inheritdoc/>
    public override SemVersion? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        var versionString = reader.GetString();

        if ( SemVersion.TryParse( versionString, SemVersionStyles.Strict, out var result ) )
        {
            return result;
        }

        throw new JsonException( "The JSON value could not be converted to SemVersion." );
    }

    /// <inheritdoc/>
    public override void Write( Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options )
    {
        if ( value == EmptyVersion )
        {
            return;
        }

        writer.WriteStringValue( value.ToString() );
    }
}
