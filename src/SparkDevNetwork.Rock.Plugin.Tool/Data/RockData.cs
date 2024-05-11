using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.Plugin.Tool.Data;

/// <summary>
/// The data that describes the Rock instance that should be installed in
/// the environment.
/// </summary>
class RockData
{
    /// <summary>
    /// The version number to install in the environment. An empty string or
    /// missing value indicates that the Rock instance will be handled manually.
    /// </summary>
    [JsonPropertyName( "version" )]
    public string? Version { get; set; }

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
