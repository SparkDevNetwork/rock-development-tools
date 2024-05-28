using System.Text.Json;

namespace SparkDevNetwork.Rock.Plugin.Tool;

/// <summary>
/// Various support elements.
/// </summary>
class Support
{
    /// <summary>
    /// A list of the supported Rock versions that can be used when generating
    /// a new plugin.
    /// </summary>
    public static readonly string[] SupportedRockVersions = ["1.16.3-rc.1", "1.16.4-rc.1"];

    /// <summary>
    /// The default serializer options that outputs with indentation.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
    {
        WriteIndented = true
    };

}
