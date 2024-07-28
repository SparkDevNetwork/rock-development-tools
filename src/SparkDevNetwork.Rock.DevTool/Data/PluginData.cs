using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The structure of the plugin file.
/// </summary>
class PluginData
{
    /// <summary>
    /// The filename to use for plugin data.
    /// </summary>
    public const string Filename = "plugin.json";

    /// <summary>
    /// The details about the organization this plugin belongs to.
    /// </summary>
    [JsonPropertyName( "organization" )]
    public OrganizationData Organization { get; set; } = new OrganizationData();

    /// <summary>
    /// The name name of this plugin.
    /// </summary>
    [JsonPropertyName( "name" )]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
