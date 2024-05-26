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
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the git repository to install the plugin from.
    /// </summary>
    [JsonPropertyName( "url" )]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The branch name to checkout in the plugin repository.
    /// </summary>
    [JsonPropertyName( "branch" )]
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
