using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.Plugin.Tool.Data;

/// <summary>
/// The data that describes a plugin that should be installed in the
/// environment.
/// </summary>
class PluginData
{
    /// <summary>
    /// The path to install the plugin into.
    /// </summary>
    [JsonPropertyName( "path" )]
    public string? Path { get; set; }

    /// <summary>
    /// The URL to the git repository to install the plugin from.
    /// </summary>
    [JsonPropertyName( "url" )]
    public string? Url { get; set; }

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
