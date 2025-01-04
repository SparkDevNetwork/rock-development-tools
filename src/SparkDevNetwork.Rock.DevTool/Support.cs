using System.Text.Json;
using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool;

/// <summary>
/// Various support elements.
/// </summary>
class Support
{
    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
