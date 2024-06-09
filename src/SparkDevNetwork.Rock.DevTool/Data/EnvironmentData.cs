using System.Text.Json.Serialization;

namespace SparkDevNetwork.Rock.DevTool.Data;

/// <summary>
/// The structure of the environment file.
/// </summary>
class EnvironmentData
{
    /// <summary>
    /// The filename to use for environment data.
    /// </summary>
    public const string Filename = "environment.json";

    /// <summary>
    /// The details about the organization this environment belongs to.
    /// </summary>
    [JsonPropertyName( "organization" )]
    public OrganizationData Organization { get; set; } = new OrganizationData();

    /// <summary>
    /// The details about the Rock instance to install in the environment.
    /// </summary>
    [JsonPropertyName( "rock" )]
    public RockData Rock { get; set; } = new RockData();

    /// <summary>
    /// The plugins that should be installed in the environment.
    /// </summary>
    [JsonPropertyName( "plugins" )]
    public List<PluginData> Plugins { get; set; } = [];

    /// <summary>
    /// Additional data in the JSON stream that we don't know about.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
